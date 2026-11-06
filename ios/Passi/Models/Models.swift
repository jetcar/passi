import Foundation

struct ApiPaths: Codable, Equatable, Sendable {
    var signup: String
    var signupConfirmation: String
    var signupCheck: String
    var tokenUpdate: String
    var cancelCheck: String
    var authorize: String
    var time: String
    var updateCertificate: String
    var checkForStartedSessions: String
    var syncAccounts: String
    var deleteAccount: String
    var listDevices: String
    var deleteDevice: String

    static let defaults = ApiPaths(
        signup: "/api/SignUp/signup",
        signupConfirmation: "/api/SignUp/confirm",
        signupCheck: "/api/SignUp/check",
        tokenUpdate: "/api/Token/Update",
        cancelCheck: "/api/Auth/Cancel",
        authorize: "/api/Auth/Authorize",
        time: "/api/Service/Time",
        updateCertificate: "/api/Certificate/UpdatePublicCert",
        checkForStartedSessions: "/api/Auth/GetActiveSession",
        syncAccounts: "/api/Auth/SyncAccounts",
        deleteAccount: "/api/Auth/Delete",
        listDevices: "/api/Auth/Devices",
        deleteDevice: "/api/Auth/DeleteDevice"
    )
}

struct Provider: Identifiable, Codable, Equatable, Hashable, Sendable {
    var id: UUID
    var name: String
    var baseURL: String
    var apiPaths: ApiPaths
    var isDefault: Bool

    static let passi = Provider(
        id: UUID(uuid: (0x7A, 0x22, 0xCC, 0x55, 0x4D, 0x18, 0x4E, 0x02,
                       0xBF, 0xA5, 0xF6, 0xCE, 0x49, 0x13, 0xFB, 0xFB)),
        name: "passi",
        baseURL: "https://passi.cloud/passiapi",
        apiPaths: .defaults,
        isDefault: true
    )

    func hash(into hasher: inout Hasher) {
        hasher.combine(id)
    }
}

struct Account: Identifiable, Codable, Equatable, Sendable {
    var id: UUID
    var providerID: UUID
    var email: String
    var deviceID: String
    var isConfirmed: Bool
    var thumbprint: String?
    var validFrom: Date
    var validTo: Date
    var inactive: Bool
    var publicCertificate: String?
    var keyTag: String?
    var pinLength: Int
    var pinSalt: String?
    var pinVerifier: String?
    var biometricsEnabled: Bool

    var isActive: Bool { !inactive }
}

struct PendingEnrollment: Codable, Equatable, Sendable {
    var accountID: UUID
    var email: String
    var providerID: UUID
    var confirmationCode: String?
}

enum ConfirmationColor: String, CaseIterable, Codable, Sendable {
    case blue
    case red
    case green
    case yellow

    init(serverValue: String) throws {
        switch serverValue.trimmingCharacters(in: .whitespacesAndNewlines).lowercased() {
        case "blue", "1": self = .blue
        case "red", "2": self = .red
        case "green", "3": self = .green
        case "yellow", "4": self = .yellow
        default: throw PassiError.invalidResponse("Unknown confirmation color")
        }
    }
}

struct NotificationSession: Identifiable, Codable, Equatable, Sendable {
    var sender: String
    var confirmationColor: ConfirmationColor
    var id: UUID
    var expirationTime: Date
    var randomString: String
    var returnHost: String
    var accountID: UUID
    var confirmationNumber: Int?
}

struct GeneratedIdentity: Sendable {
    var keyTag: String
    var publicCertificate: String
    var thumbprint: String
    var validFrom: Date
    var validTo: Date
}

enum PassiError: LocalizedError, Equatable, Sendable {
    case invalidInput(String)
    case invalidResponse(String)
    case network(String)
    case security(String)

    var errorDescription: String? {
        switch self {
        case .invalidInput(let message), .invalidResponse(let message),
             .network(let message), .security(let message):
            return message
        }
    }
}

enum ChallengeChoices {
    static func numbers(correct: Int, random: inout some RandomNumberGenerator) -> [Int] {
        var candidates = Array(10...99).filter { $0 != correct }
        candidates.shuffle(using: &random)
        var result = Array(candidates.prefix(2)) + [correct]
        result.shuffle(using: &random)
        return result
    }

    static func colors(correct: ConfirmationColor, random: inout some RandomNumberGenerator) -> [ConfirmationColor] {
        var candidates = ConfirmationColor.allCases.filter { $0 != correct }
        candidates.shuffle(using: &random)
        var result = Array(candidates.prefix(2)) + [correct]
        result.shuffle(using: &random)
        return result
    }
}
