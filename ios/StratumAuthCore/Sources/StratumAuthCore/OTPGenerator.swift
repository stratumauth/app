// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import CryptoKit
import Foundation

public enum OTPError: LocalizedError, Equatable {
    case invalidIssuer
    case invalidSecret
    case invalidDigits
    case invalidPeriod
    case invalidPIN
    case unsupportedURI
    case invalidURI(String)

    public var errorDescription: String? {
        switch self {
        case .invalidIssuer:
            return "Enter an account issuer."
        case .invalidSecret:
            return "The secret must be a valid Base32 value."
        case .invalidDigits:
            return "TOTP requires 6–10 digits and HOTP requires 6–8 digits."
        case .invalidPeriod:
            return "The time period must be greater than zero."
        case .invalidPIN:
            return "Enter a valid PIN for this authenticator."
        case .unsupportedURI:
            return "This QR code uses an unsupported authenticator format."
        case .invalidURI(let reason):
            return reason
        }
    }
}

public enum OTPGenerator {
    private static let base32Alphabet = Array("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567")

    public static func normaliseSecret(_ secret: String) -> String {
        secret
            .filter { !$0.isWhitespace && $0 != "-" }
            .uppercased()
    }

    public static func decodeBase32Secret(_ secret: String) throws -> Data {
        let normalised = normaliseSecret(secret)
        let unpadded = normalised.prefix { $0 != "=" }
        let padding = normalised.dropFirst(unpadded.count)
        let expectedPaddingByRemainder = [2: 6, 4: 4, 5: 3, 7: 1]
        let validLengthRemainders: Set<Int> = [0, 2, 4, 5, 7]
        guard padding.allSatisfy({ $0 == "=" }),
              padding.isEmpty ||
                expectedPaddingByRemainder[unpadded.count % 8] == padding.count,
              validLengthRemainders.contains(unpadded.count % 8),
              unpadded.allSatisfy({ base32Alphabet.contains($0) }) else {
            throw OTPError.invalidSecret
        }

        var output = Data()
        var buffer: UInt32 = 0
        var bitCount = 0
        for character in unpadded {
            guard let value = base32Alphabet.firstIndex(of: character) else {
                throw OTPError.invalidSecret
            }
            buffer = (buffer << 5) | UInt32(value)
            bitCount += 5
            if bitCount >= 8 {
                bitCount -= 8
                output.append(UInt8((buffer >> bitCount) & 0xff))
            }
        }

        let unusedBitMask = (UInt32(1) << bitCount) - 1
        guard !output.isEmpty, buffer & unusedBitMask == 0 else {
            throw OTPError.invalidSecret
        }
        return output
    }

    public static func generate(for authenticator: Authenticator, at date: Date = .now) throws -> String {
        try authenticator.validate()
        if authenticator.type == .mobileOtp {
            let seconds = max(0, Int64(date.timeIntervalSince1970))
            let roundedSeconds = seconds - seconds % Int64(authenticator.period)
            let material = "\(roundedSeconds / 10)\(authenticator.secret)\(authenticator.pin!)"
            let digest = Insecure.MD5.hash(data: Data(material.utf8))
            return String(digest.map { String(format: "%02x", $0) }.joined().prefix(authenticator.digits))
        }

        if authenticator.type == .yandex {
            return try yandexCode(authenticator, at: date)
        }

        let key = SymmetricKey(data: try decodeBase32Secret(authenticator.secret))
        let movingFactor: UInt64
        switch authenticator.type {
        case .totp, .steam:
            let period = authenticator.type == .steam ? 30 : authenticator.period
            movingFactor = UInt64(max(0, Int64(date.timeIntervalSince1970)) / Int64(period))
        case .hotp, .mobileOtp, .yandex:
            movingFactor = authenticator.counter
        }

        var bigEndianFactor = movingFactor.bigEndian
        let message = withUnsafeBytes(of: &bigEndianFactor) { Data($0) }
        let digest: Data
        switch authenticator.algorithm {
        case .sha1:
            digest = Data(HMAC<Insecure.SHA1>.authenticationCode(for: message, using: key))
        case .sha256:
            digest = Data(HMAC<SHA256>.authenticationCode(for: message, using: key))
        case .sha512:
            digest = Data(HMAC<SHA512>.authenticationCode(for: message, using: key))
        }

        guard let lastByte = digest.last else {
            throw OTPError.invalidSecret
        }
        let offset = Int(lastByte & 0x0f)
        let binary =
            (UInt32(digest[offset] & 0x7f) << 24) |
            (UInt32(digest[offset + 1]) << 16) |
            (UInt32(digest[offset + 2]) << 8) |
            UInt32(digest[offset + 3])
        if authenticator.type == .steam {
            let alphabet = Array("23456789BCDFGHJKMNPQRTVWXY")
            var value = binary
            var result = ""
            for _ in 0..<5 {
                result.append(alphabet[Int(value % UInt32(alphabet.count))])
                value /= UInt32(alphabet.count)
            }
            return result
        }
        let modulus = (0..<authenticator.digits).reduce(UInt64(1)) { value, _ in value * 10 }
        let code = String(UInt64(binary) % modulus)
        return String(repeating: "0", count: authenticator.digits - code.count) + code
    }

    private static func yandexCode(_ authenticator: Authenticator, at date: Date) throws -> String {
        let decoded = try decodeBase32Secret(authenticator.secret)
        var keyMaterial = Data((authenticator.pin ?? "").utf8)
        keyMaterial.append(decoded.prefix(16))
        var keyBytes = Array(SHA256.hash(data: keyMaterial))
        if keyBytes.first == 0 {
            keyBytes.removeFirst()
        }
        let key = SymmetricKey(data: Data(keyBytes))
        let factor = UInt64(max(0, Int64(date.timeIntervalSince1970)) / 30).bigEndian
        let message = withUnsafeBytes(of: factor) { Data($0) }
        let digest = Data(HMAC<SHA256>.authenticationCode(for: message, using: key))
        guard let last = digest.last else { throw OTPError.invalidSecret }
        let offset = Int(last & 0x0f)
        var material: UInt64 = 0
        for byte in digest[offset..<(offset + 8)] {
            material = (material << 8) | UInt64(byte)
        }
        material &= UInt64(Int64.max)
        let modulus: UInt64 = 26 * 26 * 26 * 26 * 26 * 26 * 26 * 26
        material %= modulus
        var result = Array(repeating: Character("a"), count: 8)
        for index in (0..<8).reversed() {
            result[index] = Character(UnicodeScalar(97 + Int(material % 26))!)
            material /= 26
        }
        return String(result)
    }
}
