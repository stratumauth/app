// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Foundation
import Security
import StratumAuthCore

enum KeychainVault {
    private static let service = Bundle.main.bundleIdentifier ?? "com.stratumauth.ios"
    private static let account = "authenticators"

    static func load() throws -> VaultData {
        var query = baseQuery
        query[kSecReturnData as String] = true
        query[kSecMatchLimit as String] = kSecMatchLimitOne

        var result: CFTypeRef?
        let status = SecItemCopyMatching(query as CFDictionary, &result)
        if status == errSecItemNotFound {
            return VaultData()
        }
        guard status == errSecSuccess, let data = result as? Data else {
            throw KeychainError.operationFailed(status)
        }

        let vault: VaultData
        do {
            vault = try JSONDecoder().decode(VaultData.self, from: data)
        } catch {
            vault = VaultData(authenticators: try JSONDecoder().decode([Authenticator].self, from: data))
        }
        for authenticator in vault.authenticators {
            try authenticator.validate()
        }
        return vault
    }

    static func save(_ vault: VaultData) throws {
        let data = try JSONEncoder().encode(vault)
        let status = SecItemUpdate(
            baseQuery as CFDictionary,
            [kSecValueData as String: data] as CFDictionary
        )
        if status == errSecItemNotFound {
            var attributes = baseQuery
            attributes[kSecValueData as String] = data
            attributes[kSecAttrAccessible as String] = kSecAttrAccessibleWhenUnlockedThisDeviceOnly
            let addStatus = SecItemAdd(attributes as CFDictionary, nil)
            guard addStatus == errSecSuccess else {
                throw KeychainError.operationFailed(addStatus)
            }
        } else if status != errSecSuccess {
            throw KeychainError.operationFailed(status)
        }
    }

    private static var baseQuery: [String: Any] {
        [
            kSecClass as String: kSecClassGenericPassword,
            kSecAttrService as String: service,
            kSecAttrAccount as String: account
        ]
    }
}

private enum KeychainError: LocalizedError {
    case operationFailed(OSStatus)

    var errorDescription: String? {
        switch self {
        case .operationFailed(let status):
            return "Secure storage could not be accessed (Keychain status \(status))."
        }
    }
}
