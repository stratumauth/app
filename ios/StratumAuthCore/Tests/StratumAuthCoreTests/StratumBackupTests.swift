// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Foundation
import StratumAuthCore
import XCTest

final class StratumBackupTests: XCTestCase {
    func testUnencryptedBackupMatchesStratumFieldNamesAndRoundTripsAllData() throws {
        let account = Authenticator(
            type: .yandex,
            issuer: "Yandex",
            username: "user",
            secret: "LA2V6KMCGYMWWVEW64RNP3JA3IAAAAAAHTSG4HRZPI",
            pin: "7586",
            ranking: 2,
            copyCount: 4
        )
        let category = AuthenticatorCategoryModel(name: "Personal")
        let icon = CustomIcon(data: Data([0x89, 0x50, 0x4e, 0x47]))
        let vault = VaultData(
            authenticators: [account],
            categories: [category],
            authenticatorCategories: [
                AuthenticatorCategory(categoryID: category.id, authenticatorID: account.id, ranking: 1)
            ],
            customIcons: [icon]
        )

        let data = try StratumBackup.export(vault)
        let json = try XCTUnwrap(String(data: data, encoding: .utf8))
        XCTAssertTrue(json.contains("\"Authenticators\""))
        XCTAssertTrue(json.contains("\"AuthenticatorCategories\""))
        XCTAssertTrue(json.contains("\"Type\":5"))
        XCTAssertTrue(json.contains("\"Algorithm\":0"))

        let restored = try StratumBackup.import(data)
        XCTAssertEqual(restored.authenticators.count, 1)
        XCTAssertEqual(restored.authenticators[0].issuer, account.issuer)
        XCTAssertEqual(restored.authenticators[0].secret, account.secret)
        XCTAssertEqual(restored.authenticators[0].pin, account.pin)
        XCTAssertEqual(restored.authenticators[0].ranking, account.ranking)
        XCTAssertEqual(restored.authenticators[0].copyCount, account.copyCount)
        XCTAssertEqual(restored.categories, vault.categories)
        XCTAssertEqual(restored.authenticatorCategories.first?.categoryID, category.id)
        XCTAssertEqual(restored.customIcons, vault.customIcons)
    }

    func testEncryptedStratumBackupRoundTripsAndRejectsWrongPassword() throws {
        let account = Authenticator(issuer: "Example", secret: "GEZDGNBVGY3TQOJQ")
        let data = try StratumBackup.export(VaultData(authenticators: [account]), password: "correct horse")

        XCTAssertTrue(data.starts(with: Data("AUTHENTICATORPRO".utf8)))
        let restored = try StratumBackup.import(data, password: "correct horse").authenticators
        XCTAssertEqual(restored.count, 1)
        XCTAssertEqual(restored[0].issuer, account.issuer)
        XCTAssertEqual(restored[0].secret, account.secret)
        XCTAssertEqual(restored[0].type, account.type)
        XCTAssertThrowsError(try StratumBackup.import(data, password: "wrong password"))
    }
}
