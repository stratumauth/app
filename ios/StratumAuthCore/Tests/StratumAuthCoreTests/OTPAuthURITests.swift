// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import StratumAuthCore
import XCTest

final class OTPAuthURITests: XCTestCase {
    func testParsesStandardTOTPURI() throws {
        let authenticator = try OTPAuthURI.parse(
            "otpauth://totp/Big%20Company%3Auser%40example.com?secret=gezDgnbvgy3tqojq&issuer=Ignored&algorithm=SHA256&digits=8&period=60"
        )

        XCTAssertEqual(authenticator.type, .totp)
        XCTAssertEqual(authenticator.issuer, "Big Company")
        XCTAssertEqual(authenticator.username, "user@example.com")
        XCTAssertEqual(authenticator.secret, "GEZDGNBVGY3TQOJQ")
        XCTAssertEqual(authenticator.algorithm, .sha256)
        XCTAssertEqual(authenticator.digits, 8)
        XCTAssertEqual(authenticator.period, 60)
    }

    func testParsesStandardHOTPURIAndCounter() throws {
        let authenticator = try OTPAuthURI.parse(
            "otpauth://hotp/user?secret=GEZDGNBVGY3TQOJQ&issuer=Example&counter=10"
        )

        XCTAssertEqual(authenticator.type, .hotp)
        XCTAssertEqual(authenticator.issuer, "Example")
        XCTAssertEqual(authenticator.username, "user")
        XCTAssertEqual(authenticator.counter, 10)
    }

    func testRejectsUnsupportedOrMalformedURIs() {
        XCTAssertThrowsError(try OTPAuthURI.parse("otpauth://yaotp/user?secret=GEZDGNBVGY3TQOJQ"))
        XCTAssertThrowsError(try OTPAuthURI.parse("otpauth://totp/user?secret=INVALID!"))
        XCTAssertThrowsError(try OTPAuthURI.parse("otpauth://totp/user?secret=GEZDGNBVGY3TQOJQ&digits=11"))
        XCTAssertThrowsError(try OTPAuthURI.parse("https://example.com"))
    }

    func testParsesSteamAndYandexURIs() throws {
        let steam = try OTPAuthURI.parse(
            "otpauth://totp/Example%3Aaccount?secret=63BEDWCQZKTQWPESARIERL5DTTQFCJTK&encoder=steam"
        )
        let yandex = try OTPAuthURI.parse(
            "otpauth://yaotp/account?secret=LA2V6KMCGYMWWVEW64RNP3JA3IAAAAAAHTSG4HRZPI&pin_length=4"
        )

        XCTAssertEqual(steam.type, .steam)
        XCTAssertEqual(steam.digits, 5)
        XCTAssertEqual(yandex.type, .yandex)
        XCTAssertEqual(yandex.issuer, "Yandex")
        XCTAssertEqual(yandex.username, "account")
    }

    func testParsesMobileOTPUriWithoutPin() throws {
        let authenticator = try OTPAuthURI.parse("motp://service:user?secret=7ac61d4736f51a2b")

        XCTAssertEqual(authenticator.type, .mobileOtp)
        XCTAssertEqual(authenticator.issuer, "service")
        XCTAssertEqual(authenticator.username, "user")
        XCTAssertEqual(authenticator.secret, "7ac61d4736f51a2b")
    }
}
