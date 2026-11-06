import Combine
import Foundation

@MainActor
final class PassiStore: ObservableObject {
    @Published private(set) var accounts: [Account]
    @Published private(set) var providers: [Provider]
    @Published var pendingEnrollment: PendingEnrollment?
    @Published var pendingSession: NotificationSession?
    @Published var isBusy = false
    @Published var errorMessage: String?
    @Published private(set) var lastPollAttemptAt: Date?
    @Published private(set) var lastPollError: String?

    let deviceID: String
    private let defaults: UserDefaults
    private let api: PassiAPI
    private let identity: IdentityService
    private let encoder: JSONEncoder
    private let decoder: JSONDecoder

    init(defaults: UserDefaults = .standard, api: PassiAPI = PassiAPI(), identity: IdentityService = IdentityService()) {
        self.defaults = defaults
        self.api = api
        self.identity = identity
        encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .iso8601

        if let storedDeviceID = defaults.string(forKey: Keys.deviceID) {
            deviceID = storedDeviceID
        } else {
            let newDeviceID = UUID().uuidString
            defaults.set(newDeviceID, forKey: Keys.deviceID)
            deviceID = newDeviceID
        }

        accounts = Self.load([Account].self, key: Keys.accounts, defaults: defaults, decoder: decoder) ?? []
        var storedProviders = Self.load([Provider].self, key: Keys.providers, defaults: defaults, decoder: decoder) ?? []
        if !storedProviders.contains(where: { $0.baseURL == Provider.passi.baseURL }) { storedProviders.append(.passi) }
        providers = storedProviders.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        pendingEnrollment = Self.load(
            PendingEnrollment.self,
            key: Keys.pendingEnrollment,
            defaults: defaults,
            decoder: decoder
        )
        persistProviders()
    }

    func saveProvider(_ provider: Provider) throws {
        let normalizedURL = provider.baseURL.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let url = URL(string: normalizedURL), url.scheme?.lowercased() == "https", url.host != nil else {
            throw PassiError.invalidInput("Provider must use a valid HTTPS URL")
        }
        var saved = provider
        saved.baseURL = normalizedURL.trimmingCharacters(in: CharacterSet(charactersIn: "/"))
        providers.removeAll { $0.id == saved.id }
        providers.append(saved)
        providers.sort { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        persistProviders()
    }

    func deleteProvider(_ provider: Provider) throws {
        guard !provider.isDefault else { throw PassiError.invalidInput("The default provider cannot be deleted") }
        guard !accounts.contains(where: { $0.providerID == provider.id }) else {
            throw PassiError.invalidInput("Delete accounts using this provider first")
        }
        providers.removeAll { $0.id == provider.id }
        persistProviders()
    }

    func beginEnrollment(email: String, providerID: UUID) async throws {
        guard email.contains("@"), let provider = provider(id: providerID) else {
            throw PassiError.invalidInput("Enter a valid email and provider")
        }
        let pending = try await api.beginSignup(email: email, provider: provider, deviceID: deviceID)
        pendingEnrollment = pending
        accounts.removeAll { !$0.isConfirmed && $0.email.caseInsensitiveCompare(email) == .orderedSame }
        accounts.append(Account(
            id: pending.accountID,
            providerID: providerID,
            email: email,
            deviceID: deviceID,
            isConfirmed: false,
            thumbprint: nil,
            validFrom: Date(),
            validTo: Date(),
            inactive: false,
            publicCertificate: nil,
            keyTag: nil,
            pinLength: 0,
            pinSalt: nil,
            pinVerifier: nil,
            biometricsEnabled: false
        ))
        persistAll()
    }

    func confirmEnrollmentCode(_ code: String) async throws {
        guard var pending = pendingEnrollment, let provider = provider(id: pending.providerID) else {
            throw PassiError.invalidInput("No pending enrollment")
        }
        try await api.checkSignupCode(pending, code: code, provider: provider)
        pending.confirmationCode = code
        pendingEnrollment = pending
        persistAll()
    }

    func finishEnrollment(pin: String, enableBiometrics: Bool) async throws {
        guard let pending = pendingEnrollment, let provider = provider(id: pending.providerID) else {
            throw PassiError.invalidInput("No pending enrollment")
        }
        if !pin.isEmpty && !(4...12).contains(pin.count) {
            throw PassiError.invalidInput("PIN must contain 4 to 12 digits")
        }
        if !pin.allSatisfy(\.isNumber) { throw PassiError.invalidInput("PIN must contain digits only") }

        let generated = try identity.createIdentity(email: pending.email, accountID: pending.accountID)
        do {
            let canonicalID = try await api.finalizeSignup(
                pending,
                provider: provider,
                deviceID: deviceID,
                publicCertificate: generated.publicCertificate
            )
            let pinMaterial = pin.isEmpty ? nil : try PinVerifier.make(pin: pin)
            if enableBiometrics { try await identity.authenticate(reason: "Enable Face ID for Passi approvals") }
            let account = Account(
                id: canonicalID,
                providerID: pending.providerID,
                email: pending.email,
                deviceID: deviceID,
                isConfirmed: true,
                thumbprint: generated.thumbprint,
                validFrom: generated.validFrom,
                validTo: generated.validTo,
                inactive: false,
                publicCertificate: generated.publicCertificate,
                keyTag: generated.keyTag,
                pinLength: pin.count,
                pinSalt: pinMaterial?.salt,
                pinVerifier: pinMaterial?.verifier,
                biometricsEnabled: enableBiometrics
            )
            accounts.removeAll { $0.id == pending.accountID || $0.id == canonicalID }
            accounts.append(account)
            pendingEnrollment = nil
            persistAll()
        } catch {
            try? identity.deleteKey(tag: generated.keyTag)
            throw error
        }
    }

    func poll(surfaceErrors: Bool = false) async {
        guard !isBusy else { return }
        let confirmedAccounts = accounts.filter(\.isConfirmed)
        guard !confirmedAccounts.isEmpty else {
            lastPollError = "Add a confirmed account before checking for login requests."
            if surfaceErrors { errorMessage = lastPollError }
            return
        }
        isBusy = true
        defer { isBusy = false }
        do {
            pendingSession = try await api.pollPendingSession(
                deviceID: deviceID,
                accounts: confirmedAccounts,
                providers: providers
            )
            lastPollAttemptAt = Date()
            lastPollError = nil
        } catch {
            lastPollAttemptAt = Date()
            lastPollError = error.localizedDescription
            if surfaceErrors { errorMessage = error.localizedDescription }
        }
    }

    func approve(_ session: NotificationSession, pin: String?, useBiometrics: Bool) async throws {
        guard let account = accounts.first(where: { $0.id == session.accountID }),
              let provider = provider(id: account.providerID),
              let keyTag = account.keyTag else {
            throw PassiError.invalidInput("Account not found for this session")
        }
        if useBiometrics {
            guard account.biometricsEnabled else { throw PassiError.security("Face ID is not enabled") }
            try await identity.authenticate(reason: "Approve this Passi login")
        } else if account.pinLength > 0 {
            guard let pin, PinVerifier.verify(pin: pin, salt: account.pinSalt, verifier: account.pinVerifier) else {
                throw PassiError.security("Invalid PIN")
            }
        }
        let signature = try identity.sign(message: session.randomString, keyTag: keyTag)
        try await api.authorize(session, account: account, provider: provider, signature: signature)
        pendingSession = nil
    }

    func cancel(_ session: NotificationSession) async throws {
        guard let account = accounts.first(where: { $0.id == session.accountID }),
              let provider = provider(id: account.providerID) else {
            throw PassiError.invalidInput("Account not found for this session")
        }
        defer { pendingSession = nil }
        try await api.cancel(session, provider: provider)
    }

    func setBiometrics(_ enabled: Bool, for account: Account, pin: String?) async throws {
        if account.pinLength > 0 {
            guard let pin, PinVerifier.verify(pin: pin, salt: account.pinSalt, verifier: account.pinVerifier) else {
                throw PassiError.security("Invalid PIN")
            }
        }
        if enabled { try await identity.authenticate(reason: "Enable Face ID for Passi approvals") }
        guard let index = accounts.firstIndex(where: { $0.id == account.id }) else { return }
        accounts[index].biometricsEnabled = enabled
        persistAccounts()
    }

    func deleteAccount(_ account: Account) async throws {
        guard let provider = provider(id: account.providerID) else { throw PassiError.invalidInput("Provider not found") }
        if account.isConfirmed { try await api.deleteAccount(account, provider: provider) }
        if let keyTag = account.keyTag { try? identity.deleteKey(tag: keyTag) }
        accounts.removeAll { $0.id == account.id }
        if pendingEnrollment?.accountID == account.id { pendingEnrollment = nil }
        persistAll()
    }

    func provider(id: UUID) -> Provider? {
        providers.first(where: { $0.id == id }) ?? providers.first(where: \.isDefault)
    }

    private func persistAll() {
        persistAccounts()
        persistProviders()
        if let pendingEnrollment, let data = try? encoder.encode(pendingEnrollment) {
            defaults.set(data, forKey: Keys.pendingEnrollment)
        } else {
            defaults.removeObject(forKey: Keys.pendingEnrollment)
        }
    }

    private func persistAccounts() {
        if let data = try? encoder.encode(accounts) { defaults.set(data, forKey: Keys.accounts) }
    }

    private func persistProviders() {
        if let data = try? encoder.encode(providers) { defaults.set(data, forKey: Keys.providers) }
    }

    private static func load<T: Decodable>(
        _ type: T.Type,
        key: String,
        defaults: UserDefaults,
        decoder: JSONDecoder
    ) -> T? {
        guard let data = defaults.data(forKey: key) else { return nil }
        return try? decoder.decode(type, from: data)
    }

    private enum Keys {
        static let accounts = "ios.accounts.v1"
        static let providers = "ios.providers.v1"
        static let pendingEnrollment = "ios.pendingEnrollment.v1"
        static let deviceID = "ios.deviceID.v1"
    }
}
