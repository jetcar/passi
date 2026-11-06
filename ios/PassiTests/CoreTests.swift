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
}

private struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64
    init(seed: UInt64) { state = seed }
    mutating func next() -> UInt64 {
        state = state &* 6_364_136_223_846_793_005 &+ 1
        return state
    }
}
