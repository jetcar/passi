import Foundation

actor PassiAPI {
    // URLSession async requests: https://developer.apple.com/documentation/foundation/urlsession
    private let session: URLSession
    private let encoder = JSONEncoder()

    init(session: URLSession = .shared) {
        self.session = session
    }

    func beginSignup(email: String, provider: Provider, deviceID: String) async throws -> PendingEnrollment {
        let accountID = UUID()
        let body = SignupRequest(email: email, deviceID: deviceID, userGuid: accountID.uuidString)
        _ = try await send(provider: provider, path: provider.apiPaths.signup, method: "POST", body: body)
        return PendingEnrollment(accountID: accountID, email: email, providerID: provider.id)
    }

    func checkSignupCode(_ pending: PendingEnrollment, code: String, provider: Provider) async throws {
        let body = SignupCheckRequest(username: pending.email, code: code)
        _ = try await send(provider: provider, path: provider.apiPaths.signupCheck, method: "POST", body: body)
    }

    func finalizeSignup(
        _ pending: PendingEnrollment,
        provider: Provider,
        deviceID: String,
        publicCertificate: String
    ) async throws -> UUID {
        guard let code = pending.confirmationCode else {
            throw PassiError.invalidInput("Missing confirmation code")
        }
        let body = SignupConfirmationRequest(
            email: pending.email,
            code: code,
            publicCert: publicCertificate,
            guid: pending.accountID.uuidString,
            deviceID: deviceID
        )
        let data = try await send(
            provider: provider,
            path: provider.apiPaths.signupConfirmation,
            method: "POST",
            body: body
        )
        guard !data.isEmpty,
              let response = try? JSONDecoder().decode(SignupConfirmationResponse.self, from: data),
              let rawID = response.accountGuid,
              let canonicalID = UUID(uuidString: rawID) else {
            return pending.accountID
        }
        return canonicalID
    }

    func pollPendingSession(
        deviceID: String,
        accounts: [Account],
        providers: [Provider]
    ) async throws -> NotificationSession? {
        for (providerID, providerAccounts) in Dictionary(grouping: accounts, by: \.providerID) {
            guard let provider = providers.first(where: { $0.id == providerID }) ?? providers.first(where: \.isDefault) else {
                continue
            }

            let sync = SyncAccountsRequest(
                deviceID: deviceID,
                guids: providerAccounts.map { $0.id.uuidString }
            )
            _ = try? await send(provider: provider, path: provider.apiPaths.syncAccounts, method: "POST", body: sync)

            let request = GetAllSessionRequest(deviceID: deviceID)
            let data = try await send(
                provider: provider,
                path: provider.apiPaths.checkForStartedSessions,
                method: "POST",
                body: request
            )
            if data.isEmpty || String(data: data, encoding: .utf8) == "null" {
                continue
            }
            return try Self.decodeNotification(data)
        }
        return nil
    }

    func authorize(_ session: NotificationSession, account: Account, provider: Provider, signature: String) async throws {
        let body = AuthorizeRequest(
            signedHash: signature,
            publicCertThumbprint: account.thumbprint ?? "",
            sessionID: session.id.uuidString
        )
        _ = try await send(provider: provider, path: provider.apiPaths.authorize, method: "POST", body: body)
    }

    func cancel(_ session: NotificationSession, provider: Provider) async throws {
        _ = try await send(
            provider: provider,
            path: provider.apiPaths.cancelCheck,
            method: "GET",
            queryItems: [URLQueryItem(name: "SessionId", value: session.id.uuidString)]
        )
    }

    func deleteAccount(_ account: Account, provider: Provider) async throws {
        guard let thumbprint = account.thumbprint else {
            if account.isConfirmed { throw PassiError.invalidInput("Certificate thumbprint is missing") }
            return
        }
        _ = try await send(
            provider: provider,
            path: provider.apiPaths.deleteAccount,
            method: "DELETE",
            queryItems: [
                URLQueryItem(name: "accountGuid", value: account.id.uuidString),
                URLQueryItem(name: "thumbprint", value: thumbprint)
            ]
        )
    }

    static func makeURL(provider: Provider, path: String, queryItems: [URLQueryItem] = []) throws -> URL {
        let base = provider.baseURL.trimmingCharacters(in: CharacterSet(charactersIn: "/"))
        let suffix = path.hasPrefix("/") ? path : "/" + path
        guard var components = URLComponents(string: base + suffix), components.scheme?.lowercased() == "https" else {
            throw PassiError.invalidInput("Provider must use HTTPS")
        }
        if !queryItems.isEmpty { components.queryItems = queryItems }
        guard let url = components.url else { throw PassiError.invalidInput("Invalid provider URL") }
        return url
    }

    static func decodeNotification(_ data: Data) throws -> NotificationSession {
        let dto = try JSONDecoder().decode(NotificationResponse.self, from: data)
        guard let sessionID = UUID(uuidString: dto.sessionID),
              let accountID = UUID(uuidString: dto.accountGuid),
              let expiration = parseDate(dto.expirationTime) else {
            throw PassiError.invalidResponse("Invalid pending session response")
        }
        return NotificationSession(
            sender: dto.sender,
            confirmationColor: try ConfirmationColor(serverValue: dto.confirmationColor.value),
            id: sessionID,
            expirationTime: expiration,
            randomString: dto.randomString,
            returnHost: dto.returnHost,
            accountID: accountID,
            confirmationNumber: dto.confirmationNumber
        )
    }

    private static func parseDate(_ value: String) -> Date? {
        let fractional = ISO8601DateFormatter()
        fractional.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let date = fractional.date(from: value) { return date }
        return ISO8601DateFormatter().date(from: value)
    }

    private func send<Body: Encodable>(
        provider: Provider,
        path: String,
        method: String,
        body: Body,
        queryItems: [URLQueryItem] = []
    ) async throws -> Data {
        try await send(
            provider: provider,
            path: path,
            method: method,
            bodyData: try encoder.encode(body),
            queryItems: queryItems
        )
    }

    private func send(
        provider: Provider,
        path: String,
        method: String,
        queryItems: [URLQueryItem] = []
    ) async throws -> Data {
        try await send(provider: provider, path: path, method: method, bodyData: nil, queryItems: queryItems)
    }

    private func send(
        provider: Provider,
        path: String,
        method: String,
        bodyData: Data?,
        queryItems: [URLQueryItem]
    ) async throws -> Data {
        let url = try Self.makeURL(provider: provider, path: path, queryItems: queryItems)
        var request = URLRequest(url: url, timeoutInterval: 30)
        request.httpMethod = method
        request.httpBody = bodyData
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        if bodyData != nil { request.setValue("application/json", forHTTPHeaderField: "Content-Type") }

        do {
            let (data, response) = try await session.data(for: request)
            guard let http = response as? HTTPURLResponse else {
                throw PassiError.network("Invalid server response")
            }
            guard 200..<300 ~= http.statusCode else {
                throw PassiError.network(Self.errorMessage(from: data) ?? "Network error. Try again")
            }
            return data
        } catch let error as PassiError {
            throw error
        } catch {
            throw PassiError.network("Cannot reach server. Check your connection and provider URL")
        }
    }

    private static func errorMessage(from data: Data) -> String? {
        guard let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any] else { return nil }
        return object["errors"] as? String ?? object["Errors"] as? String
    }
}

private struct SignupRequest: Encodable {
    let email: String
    let deviceID: String
    let userGuid: String
    enum CodingKeys: String, CodingKey { case email = "Email", deviceID = "DeviceId", userGuid = "UserGuid" }
}

private struct SignupCheckRequest: Encodable {
    let username: String
    let code: String
    enum CodingKeys: String, CodingKey { case username = "Username", code = "Code" }
}

private struct SignupConfirmationRequest: Encodable {
    let email: String
    let code: String
    let publicCert: String
    let guid: String
    let deviceID: String
    enum CodingKeys: String, CodingKey {
        case email = "Email", code = "Code", publicCert = "PublicCert", guid = "Guid", deviceID = "DeviceId"
    }
}

private struct SignupConfirmationResponse: Decodable {
    let accountGuid: String?
    enum CodingKeys: String, CodingKey { case accountGuid = "AccountGuid" }
}

private struct SyncAccountsRequest: Encodable {
    let deviceID: String
    let guids: [String]
    enum CodingKeys: String, CodingKey { case deviceID = "DeviceId", guids = "Guids" }
}

private struct GetAllSessionRequest: Encodable {
    let deviceID: String
    enum CodingKeys: String, CodingKey { case deviceID = "DeviceId" }
}

private struct AuthorizeRequest: Encodable {
    let signedHash: String
    let publicCertThumbprint: String
    let sessionID: String
    enum CodingKeys: String, CodingKey {
        case signedHash = "SignedHash", publicCertThumbprint = "PublicCertThumbprint", sessionID = "SessionId"
    }
}

private struct NotificationResponse: Decodable {
    let sender: String
    let confirmationColor: StringOrInt
    let confirmationNumber: Int?
    let sessionID: String
    let expirationTime: String
    let randomString: String
    let returnHost: String
    let accountGuid: String
    enum CodingKeys: String, CodingKey {
        case sender = "Sender", confirmationColor = "ConfirmationColor", confirmationNumber = "ConfirmationNumber"
        case sessionID = "SessionId", expirationTime = "ExpirationTime", randomString = "RandomString"
        case returnHost = "ReturnHost", accountGuid = "AccountGuid"
    }
}

private struct StringOrInt: Decodable {
    let value: String

    init(from decoder: Decoder) throws {
        let container = try decoder.singleValueContainer()
        if let string = try? container.decode(String.self) { value = string; return }
        if let integer = try? container.decode(Int.self) { value = String(integer); return }
        throw PassiError.invalidResponse("Invalid confirmation color")
    }
}
