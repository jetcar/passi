import Foundation
import XCTest
@testable import Passi

final class CoreTests: XCTestCase {
    func testDefaultProviderUsesProductionHTTPS() throws {
        let url = try PassiAPI.makeURL(provider: .passi, path: ApiPaths.defaults.signup)
        XCTAssertEqual(url.absoluteString, "https://passi.cloud/passiapi/api/SignUp/signup")
    }

    func testNotificationDecodesNumericColor() throws {
        let data = Data("""
        {
          "Sender":"Safari",
          "ConfirmationColor":2,
          "ConfirmationNumber":42,
          "SessionId":"11111111-1111-1111-1111-111111111111",
          "ExpirationTime":"2026-11-06T12:34:56.000Z",
          "RandomString":"challenge",
          "ReturnHost":"passi.cloud",
          "AccountGuid":"22222222-2222-2222-2222-222222222222"
        }
        """.utf8)

        let session = try PassiAPI.decodeNotification(data)
        XCTAssertEqual(session.confirmationColor, .red)
        XCTAssertEqual(session.confirmationNumber, 42)
    }

    func testPinVerifierRejectsWrongPin() throws {
        let material = try PinVerifier.make(pin: "1234")
        XCTAssertTrue(PinVerifier.verify(pin: "1234", salt: material.salt, verifier: material.verifier))
        XCTAssertFalse(PinVerifier.verify(pin: "4321", salt: material.salt, verifier: material.verifier))
    }

    func testChallengeChoicesContainCorrectAnswerOnce() {
        var random = SeededGenerator(seed: 7)
        let colors = ChallengeChoices.colors(correct: .green, random: &random)
        XCTAssertEqual(colors.count, 3)
        XCTAssertEqual(colors.filter { $0 == .green }.count, 1)

        let numbers = ChallengeChoices.numbers(correct: 42, random: &random)
        XCTAssertEqual(numbers.count, 3)
        XCTAssertEqual(numbers.filter { $0 == 42 }.count, 1)
    }

    func testPollingTreatsEmptyServerObjectAsNoActiveSession() async throws {
        PollingURLProtocol.reset()
        let configuration = URLSessionConfiguration.ephemeral
        configuration.protocolClasses = [PollingURLProtocol.self]
        let api = PassiAPI(session: URLSession(configuration: configuration))
        let accountID = UUID(uuidString: "22222222-2222-2222-2222-222222222222")!
        let account = Account(
            id: accountID,
            providerID: Provider.passi.id,
            email: "user@example.com",
            deviceID: "device-1",
            isConfirmed: true,
            thumbprint: "thumbprint",
            validFrom: Date(),
            validTo: Date().addingTimeInterval(86_400),
            inactive: false,
            publicCertificate: nil,
            keyTag: nil,
            pinLength: 0,
            pinSalt: nil,
            pinVerifier: nil,
            biometricsEnabled: false
        )

        let result = try await api.pollPendingSession(
            deviceID: "device-1",
            accounts: [account],
            providers: [.passi]
        )

        XCTAssertNil(result)
        XCTAssertEqual(PollingURLProtocol.requestPaths, [
            "/passiapi/api/Auth/SyncAccounts",
            "/passiapi/api/Auth/GetActiveSession"
        ])
    }
}

private final class PollingURLProtocol: URLProtocol {
    nonisolated(unsafe) private static var recordedPaths: [String] = []
    private static let lock = NSLock()

    static var requestPaths: [String] {
        lock.withLock { recordedPaths }
    }

    static func reset() {
        lock.withLock { recordedPaths = [] }
    }

    override class func canInit(with request: URLRequest) -> Bool { true }
    override class func canonicalRequest(for request: URLRequest) -> URLRequest { request }

    override func startLoading() {
        guard let url = request.url else {
            client?.urlProtocol(self, didFailWithError: URLError(.badURL))
            return
        }
        Self.lock.withLock { Self.recordedPaths.append(url.path) }
        let body = url.path.hasSuffix("SyncAccounts") ? Data("[]".utf8) : Data("{}".utf8)
        let response = HTTPURLResponse(url: url, statusCode: 200, httpVersion: nil, headerFields: nil)!
        client?.urlProtocol(self, didReceive: response, cacheStoragePolicy: .notAllowed)
        client?.urlProtocol(self, didLoad: body)
        client?.urlProtocolDidFinishLoading(self)
    }

    override func stopLoading() {}
}

private struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64
    init(seed: UInt64) { state = seed }
    mutating func next() -> UInt64 {
        state = state &* 6_364_136_223_846_793_005 &+ 1
        return state
    }
}
