import CryptoKit
import Foundation
import LocalAuthentication
import Security
import SwiftASN1
import X509

@MainActor
final class IdentityService {
    func createIdentity(email: String, accountID: UUID) throws -> GeneratedIdentity {
        // RSA key generation and Keychain persistence:
        // https://developer.apple.com/documentation/security/generating-new-cryptographic-keys
        let keyTag = "com.passi.cloud.ios.key.\(accountID.uuidString.lowercased())"
        try deleteKey(tag: keyTag)

        let attributes: [String: Any] = [
            kSecAttrKeyType as String: kSecAttrKeyTypeRSA,
            kSecAttrKeySizeInBits as String: 2048,
            kSecPrivateKeyAttrs as String: [
                kSecAttrIsPermanent as String: true,
                kSecAttrApplicationTag as String: Data(keyTag.utf8),
                kSecAttrAccessible as String: kSecAttrAccessibleWhenUnlockedThisDeviceOnly
            ]
        ]
        var creationError: Unmanaged<CFError>?
        guard let secKey = SecKeyCreateRandomKey(attributes as CFDictionary, &creationError) else {
            let message = creationError?.takeRetainedValue().localizedDescription ?? "Key generation failed"
            throw PassiError.security(message)
        }

        do {
            let privateKey = try Certificate.PrivateKey(secKey)
            let commonName = email.replacingOccurrences(of: "@", with: "")
            let name = try DistinguishedName { CommonName(commonName) }
            let validFrom = Date().addingTimeInterval(-86_400)
            let validTo = Date().addingTimeInterval(365 * 86_400)
            let certificate = try Certificate(
                version: .v3,
                serialNumber: .init(),
                publicKey: privateKey.publicKey,
                notValidBefore: validFrom,
                notValidAfter: validTo,
                issuer: name,
                subject: name,
                signatureAlgorithm: .sha256WithRSAEncryption,
                extensions: Certificate.Extensions {
                    Critical(BasicConstraints.notCertificateAuthority)
                    KeyUsage(digitalSignature: true)
                },
                issuerPrivateKey: privateKey
            )
            var serializer = DER.Serializer()
            try serializer.serialize(certificate)
            let certificateData = Data(serializer.serializedBytes)
            let thumbprint = Insecure.SHA1.hash(data: certificateData)
                .map { String(format: "%02X", $0) }
                .joined()

            return GeneratedIdentity(
                keyTag: keyTag,
                publicCertificate: certificateData.base64EncodedString(),
                thumbprint: thumbprint,
                validFrom: validFrom,
                validTo: validTo
            )
        } catch {
            try? deleteKey(tag: keyTag)
            throw error
        }
    }

    func sign(message: String, keyTag: String) throws -> String {
        let key = try findKey(tag: keyTag)
        var signingError: Unmanaged<CFError>?
        guard let signature = SecKeyCreateSignature(
            key,
            .rsaSignatureMessagePKCS1v15SHA512,
            Data(message.utf8) as CFData,
            &signingError
        ) as Data? else {
            let message = signingError?.takeRetainedValue().localizedDescription ?? "Signing failed"
            throw PassiError.security(message)
        }
        return signature.base64EncodedString()
    }

    func deleteKey(tag: String) throws {
        let status = SecItemDelete([
            kSecClass as String: kSecClassKey,
            kSecAttrApplicationTag as String: Data(tag.utf8),
            kSecAttrKeyType as String: kSecAttrKeyTypeRSA
        ] as CFDictionary)
        guard status == errSecSuccess || status == errSecItemNotFound else {
            throw PassiError.security("Unable to remove private key (\(status))")
        }
    }

    func authenticate(reason: String) async throws {
        // https://developer.apple.com/documentation/localauthentication/lacontext
        let context = LAContext()
        var evaluationError: NSError?
        guard context.canEvaluatePolicy(.deviceOwnerAuthenticationWithBiometrics, error: &evaluationError) else {
            throw PassiError.security(evaluationError?.localizedDescription ?? "Face ID is not available")
        }
        guard try await context.evaluatePolicy(
            .deviceOwnerAuthenticationWithBiometrics,
            localizedReason: reason
        ) else {
            throw PassiError.security("Face ID was not confirmed")
        }
    }

    private func findKey(tag: String) throws -> SecKey {
        var item: CFTypeRef?
        let status = SecItemCopyMatching([
            kSecClass as String: kSecClassKey,
            kSecAttrApplicationTag as String: Data(tag.utf8),
            kSecAttrKeyType as String: kSecAttrKeyTypeRSA,
            kSecReturnRef as String: true,
            kSecMatchLimit as String: kSecMatchLimitOne
        ] as CFDictionary, &item)
        guard status == errSecSuccess, let key = item as! SecKey? else {
            throw PassiError.security("Private key not found")
        }
        return key
    }
}

enum PinVerifier {
    private static let rounds = 50_000

    static func make(pin: String) throws -> (salt: String, verifier: String) {
        var salt = Data(count: 16)
        let status = salt.withUnsafeMutableBytes { bytes in
            guard let address = bytes.baseAddress else { return errSecParam }
            return SecRandomCopyBytes(kSecRandomDefault, bytes.count, address)
        }
        guard status == errSecSuccess else { throw PassiError.security("PIN salt generation failed") }
        return (salt.base64EncodedString(), derive(pin: pin, salt: salt).base64EncodedString())
    }

    static func verify(pin: String, salt: String?, verifier: String?) -> Bool {
        guard let salt, let verifier,
              let saltData = Data(base64Encoded: salt),
              let expected = Data(base64Encoded: verifier) else { return false }
        let actual = derive(pin: pin, salt: saltData)
        guard actual.count == expected.count else { return false }
        return zip(actual, expected).reduce(0) { $0 | ($1.0 ^ $1.1) } == 0
    }

    private static func derive(pin: String, salt: Data) -> Data {
        var value = salt + Data(pin.utf8)
        for _ in 0..<rounds { value = Data(SHA256.hash(data: value)) }
        return value
    }
}
