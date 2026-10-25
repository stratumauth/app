// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Foundation
import StratumAuthCore
import XCTest

final class OTPGeneratorTests: XCTestCase {
    func testRFC4226HOTPTestVectors() throws {
        let secret = base32(Data("12345678901234567890".utf8))
        let expected = [
            "755224", "287082", "359152", "969429", "338314",
            "254676", "287922", "162583", "399871", "520489"
        ]

        for (counter, code) in expected.enumerated() {
            let authenticator = Authenticator(
                type: .hotp,
                issuer: "RFC 4226",
                secret: secret,
                counter: UInt64(counter)
            )
            XCTAssertEqual(try OTPGenerator.generate(for: authenticator), code)
        }
    }

    func testRFC6238TOTPTestVectorsForAllAlgorithms() throws {
        let vectors: [(OTPAlgorithm, String, String)] = [
            (.sha1, "12345678901234567890", "94287082"),
            (.sha256, "12345678901234567890123456789012", "46119246"),
            (.sha512, "1234567890123456789012345678901234567890123456789012345678901234", "90693936")
        ]

        for (algorithm, key, expectedCode) in vectors {
            let authenticator = Authenticator(
                issuer: "RFC 6238",
                secret: base32(Data(key.utf8)),
                algorithm: algorithm,
                digits: 8
            )
            XCTAssertEqual(
                try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: 59)),
                expectedCode
            )
        }
    }

    func testTOTPUsesConfiguredPeriod() throws {
        let authenticator = Authenticator(
            issuer: "Example",
            secret: base32(Data("12345678901234567890".utf8)),
            period: 60
        )
        let atTheEndOfFirstWindow = try OTPGenerator.generate(
            for: authenticator,
            at: Date(timeIntervalSince1970: 59)
        )
        let atTheStartOfSecondWindow = try OTPGenerator.generate(
            for: authenticator,
            at: Date(timeIntervalSince1970: 60)
        )

        XCTAssertEqual(atTheEndOfFirstWindow, "755224")
        XCTAssertEqual(atTheStartOfSecondWindow, "287082")
    }

    func testSecretNormalisationRemovesSpacesAndHyphens() throws {
        let expectedSecret = base32(Data("12345678901234567890".utf8))
        let lowercasedSecret = expectedSecret.lowercased()
        let formatted = String(lowercasedSecret.prefix(8)) + " - " + String(lowercasedSecret.dropFirst(8))
        let authenticator = Authenticator(issuer: "Example", secret: String(formatted))

        XCTAssertEqual(authenticator.secret, expectedSecret)
        XCTAssertEqual(try OTPGenerator.generate(for: authenticator), "755224")
    }

    func testInvalidSecretsAndParametersAreRejected() {
        XCTAssertThrowsError(try OTPGenerator.decodeBase32Secret("INVALID!"))
        XCTAssertThrowsError(try OTPGenerator.decodeBase32Secret("MY====="))
        XCTAssertThrowsError(try OTPGenerator.decodeBase32Secret("MZ======"))
        XCTAssertThrowsError(try Authenticator(issuer: "Example", secret: "ABC", digits: 5).validate())
        XCTAssertThrowsError(try Authenticator(issuer: " ", secret: "MY======").validate())
    }

    func testOlderVaultAccountRecordsDecodeWithDefaultsForNewFields() throws {
        let json = Data(
            """
            {"id":"00000000-0000-0000-0000-000000000001","type":"totp","issuer":"Example","username":null,"secret":"GEZDGNBVGY3TQOJQ","algorithm":"SHA1","digits":6,"period":30,"counter":0}
            """.utf8
        )
        let authenticator = try JSONDecoder().decode(Authenticator.self, from: json)

        XCTAssertNil(authenticator.pin)
        XCTAssertNil(authenticator.icon)
        XCTAssertEqual(authenticator.ranking, 0)
        XCTAssertEqual(authenticator.copyCount, 0)
        XCTAssertNoThrow(try authenticator.validate())
    }

    func testMobileOTPTestVectors() throws {
        let authenticator = Authenticator(
            type: .mobileOtp,
            issuer: "mOTP",
            username: "test",
            secret: "7ac61d4736f51a2b",
            pin: "5555"
        )
        XCTAssertEqual(try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: 0)), "c42a26")
        XCTAssertEqual(
            try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: 1_000_000_000)),
            "2783ae"
        )
    }

    func testSteamOTPTestVectors() throws {
        let authenticator = Authenticator(
            type: .steam,
            issuer: "Steam",
            secret: "63BEDWCQZKTQWPESARIERL5DTTQFCJTK"
        )
        XCTAssertEqual(
            try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: 1_511_200_518)),
            "FR8RV"
        )
        XCTAssertEqual(
            try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: 1_511_200_714)),
            "9P3VP"
        )
    }

    func testYandexOTPTestVectors() throws {
        let vectors: [(String, String, TimeInterval, String)] = [
            ("LA2V6KMCGYMWWVEW64RNP3JA3IAAAAAAHTSG4HRZPI", "7586", 1_581_064_020, "oactmacq"),
            ("LA2V6KMCGYMWWVEW64RNP3JA3IAAAAAAHTSG4HRZPI", "7586", 1_581_090_810, "wemdwrix"),
            ("JBGSAU4G7IEZG6OY4UAXX62JU4AAAAAAHTSG4HXU3M", "5210481216086702", 1_581_091_469, "dfrpywob"),
            ("JBGSAU4G7IEZG6OY4UAXX62JU4AAAAAAHTSG4HXU3M", "5210481216086702", 1_581_093_059, "vunyprpd")
        ]

        for (secret, pin, timestamp, expected) in vectors {
            let authenticator = Authenticator(type: .yandex, issuer: "Yandex", secret: secret, pin: pin)
            XCTAssertEqual(try OTPGenerator.generate(for: authenticator, at: Date(timeIntervalSince1970: timestamp)), expected)
        }
    }

    private func base32(_ data: Data) -> String {
        let alphabet = Array("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567")
        var output = ""
        var buffer: UInt32 = 0
        var bitCount = 0

        for byte in data {
            buffer = (buffer << 8) | UInt32(byte)
            bitCount += 8
            while bitCount >= 5 {
                bitCount -= 5
                output.append(alphabet[Int((buffer >> bitCount) & 0x1f)])
            }
        }
        if bitCount > 0 {
            output.append(alphabet[Int((buffer << (5 - bitCount)) & 0x1f)])
        }
        while output.count % 8 != 0 {
            output.append("=")
        }
        return output
    }
}
