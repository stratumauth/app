// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Argon2idSwiftNative
import CommonCrypto
import CryptoKit
import Foundation

public struct AuthenticatorCategory: Codable, Equatable, Identifiable, Sendable {
    public var categoryID: String
    public var authenticatorID: UUID
    public var ranking: Int

    public var id: String { "\(categoryID):\(authenticatorID)" }

    public init(categoryID: String, authenticatorID: UUID, ranking: Int = 0) {
        self.categoryID = categoryID
        self.authenticatorID = authenticatorID
        self.ranking = ranking
    }
}

public struct AuthenticatorCategoryModel: Codable, Equatable, Identifiable, Sendable {
    public var id: String
    public var name: String
    public var ranking: Int

    public init(name: String, ranking: Int = 0) {
        let name = String(name.trimmingCharacters(in: .whitespacesAndNewlines).prefix(32))
        self.name = name
        self.ranking = ranking
        self.id = Self.id(for: name)
    }

    public init(id: String, name: String, ranking: Int = 0) {
        self.id = id
        self.name = name
        self.ranking = ranking
    }

    public static func id(for name: String) -> String {
        SHA1.hash(data: Data(name.utf8))
            .map { String(format: "%02x", $0) }
            .joined()
            .prefix(8)
            .description
    }
}

public struct CustomIcon: Codable, Equatable, Identifiable, Sendable {
    public var id: String
    public var data: Data

    public init(data: Data) {
        self.data = data
        self.id = SHA1.hash(data: data)
            .map { String(format: "%02x", $0) }
            .joined()
            .prefix(8)
            .description
    }

    public init(id: String, data: Data) {
        self.id = id
        self.data = data
    }
}

public struct VaultData: Codable, Equatable, Sendable {
    public var authenticators: [Authenticator]
    public var categories: [AuthenticatorCategoryModel]
    public var authenticatorCategories: [AuthenticatorCategory]
    public var customIcons: [CustomIcon]

    public init(
        authenticators: [Authenticator] = [],
        categories: [AuthenticatorCategoryModel] = [],
        authenticatorCategories: [AuthenticatorCategory] = [],
        customIcons: [CustomIcon] = []
    ) {
        self.authenticators = authenticators
        self.categories = categories
        self.authenticatorCategories = authenticatorCategories
        self.customIcons = customIcons
    }
}

public enum StratumBackupError: LocalizedError, Equatable, Sendable {
    case invalidFile
    case invalidPassword
    case invalidData(String)

    public var errorDescription: String? {
        switch self {
        case .invalidFile:
            return "This file is not a supported Stratum backup."
        case .invalidPassword:
            return "The backup password is incorrect or the backup is damaged."
        case .invalidData(let detail):
            return detail
        }
    }
}

public enum StratumBackup {
    private static let strongHeader = Data("AUTHENTICATORPRO".utf8)
    private static let legacyHeader = Data("AuthenticatorPro".utf8)
    private static let strongHeaderLength = 16
    private static let strongSaltLength = 16
    private static let strongNonceLength = 12
    private static let strongTagLength = 16

    public static func export(_ vault: VaultData, password: String? = nil) throws -> Data {
        let backup = try LegacyBackup(vault: vault)
        let json = try JSONEncoder().encode(backup)
        guard let password else {
            return json
        }
        guard !password.isEmpty else {
            throw StratumBackupError.invalidPassword
        }

        let salt = Data((0..<strongSaltLength).map { _ in UInt8.random(in: .min ... .max) })
        let nonceData = Data((0..<strongNonceLength).map { _ in UInt8.random(in: .min ... .max) })
        let nonce = try AES.GCM.Nonce(data: nonceData)
        let key = try deriveStrongKey(password: password, salt: salt)
        let sealed = try AES.GCM.seal(json, using: key, nonce: nonce)
        var output = strongHeader
        output.append(salt)
        output.append(nonceData)
        output.append(sealed.ciphertext)
        output.append(sealed.tag)
        return output
    }

    public static func `import`(_ data: Data, password: String? = nil) throws -> VaultData {
        let json: Data
        if data.starts(with: strongHeader) {
            guard let password, !password.isEmpty else {
                throw StratumBackupError.invalidPassword
            }
            guard data.count >= strongHeaderLength + strongSaltLength + strongNonceLength + strongTagLength else {
                throw StratumBackupError.invalidFile
            }
            let saltStart = strongHeaderLength
            let nonceStart = saltStart + strongSaltLength
            let payloadStart = nonceStart + strongNonceLength
            let tagStart = data.count - strongTagLength
            let salt = data[saltStart..<nonceStart]
            let nonceData = data[nonceStart..<payloadStart]
            let nonce = try AES.GCM.Nonce(data: nonceData)
            let ciphertext = data[payloadStart..<tagStart]
            let tag = data[tagStart...]
            let key = try deriveStrongKey(password: password, salt: Data(salt))
            do {
                json = try AES.GCM.open(
                    AES.GCM.SealedBox(nonce: nonce, ciphertext: ciphertext, tag: tag),
                    using: key
                )
            } catch {
                throw StratumBackupError.invalidPassword
            }
        } else if data.starts(with: legacyHeader) {
            guard let password, !password.isEmpty else {
                throw StratumBackupError.invalidPassword
            }
            json = try decryptLegacy(data, password: password)
        } else {
            json = data
        }

        do {
            let backup = try JSONDecoder().decode(LegacyBackup.self, from: json)
            return try backup.makeVault()
        } catch let error as StratumBackupError {
            throw error
        } catch {
            throw StratumBackupError.invalidData("The Stratum backup contains invalid or unsupported data.")
        }
    }

    private static func deriveStrongKey(password: String, salt: Data) throws -> SymmetricKey {
        try Argon2id.deriveKey(
            password: Data(password.utf8),
            salt: salt,
            memoryKiB: 65_536,
            iterations: 3,
            parallelism: 4,
            outputByteCount: 32
        )
    }

    private static func decryptLegacy(_ data: Data, password: String) throws -> Data {
        let headerLength = legacyHeader.count
        let saltLength = 20
        let ivLength = 16
        let prefixLength = headerLength + saltLength + ivLength
        guard data.count > prefixLength,
              (data.count - prefixLength).isMultiple(of: kCCBlockSizeAES128) else {
            throw StratumBackupError.invalidFile
        }
        let salt = data[headerLength..<(headerLength + saltLength)]
        let iv = data[(headerLength + saltLength)..<prefixLength]
        let ciphertext = data[prefixLength...]
        let passwordData = Data(password.utf8)
        var key = Data(count: 32)
        let derivationStatus = key.withUnsafeMutableBytes { keyBytes in
            passwordData.withUnsafeBytes { passwordBytes in
                salt.withUnsafeBytes { saltBytes in
                    CCKeyDerivationPBKDF(
                        CCPBKDFAlgorithm(kCCPBKDF2),
                        passwordBytes.bindMemory(to: Int8.self).baseAddress,
                        passwordData.count,
                        saltBytes.bindMemory(to: UInt8.self).baseAddress,
                        salt.count,
                        CCPseudoRandomAlgorithm(kCCPRFHmacAlgSHA1),
                        64_000,
                        keyBytes.bindMemory(to: UInt8.self).baseAddress,
                        key.count
                    )
                }
            }
        }
        guard derivationStatus == kCCSuccess else {
            throw StratumBackupError.invalidFile
        }

        var plaintext = Data(count: ciphertext.count)
        var movedBytes = 0
        let cryptStatus = plaintext.withUnsafeMutableBytes { plaintextBytes in
            ciphertext.withUnsafeBytes { ciphertextBytes in
                iv.withUnsafeBytes { ivBytes in
                    key.withUnsafeBytes { keyBytes in
                        CCCrypt(
                            CCOperation(kCCDecrypt),
                            CCAlgorithm(kCCAlgorithmAES),
                            CCOptions(kCCOptionPKCS7Padding),
                            keyBytes.bindMemory(to: UInt8.self).baseAddress,
                            key.count,
                            ivBytes.bindMemory(to: UInt8.self).baseAddress,
                            ciphertextBytes.bindMemory(to: UInt8.self).baseAddress,
                            ciphertext.count,
                            plaintextBytes.bindMemory(to: UInt8.self).baseAddress,
                            plaintext.count,
                            &movedBytes
                        )
                    }
                }
            }
        }
        guard cryptStatus == kCCSuccess else {
            throw StratumBackupError.invalidPassword
        }
        return Data(plaintext.prefix(movedBytes))
    }
}

private struct LegacyBackup: Codable {
    var authenticators: [LegacyAuthenticator]
    var categories: [LegacyCategory]
    var authenticatorCategories: [LegacyAuthenticatorCategory]
    var customIcons: [LegacyCustomIcon]

    enum CodingKeys: String, CodingKey {
        case authenticators = "Authenticators"
        case categories = "Categories"
        case authenticatorCategories = "AuthenticatorCategories"
        case customIcons = "CustomIcons"
    }

    init(vault: VaultData) throws {
        authenticators = try vault.authenticators.map(LegacyAuthenticator.init)
        categories = vault.categories.map(LegacyCategory.init)
        let secretByID = Dictionary(uniqueKeysWithValues: vault.authenticators.map { ($0.id, $0.secret) })
        authenticatorCategories = try vault.authenticatorCategories.map { binding in
            guard let secret = secretByID[binding.authenticatorID] else {
                throw StratumBackupError.invalidData("A category refers to an account that no longer exists.")
            }
            return LegacyAuthenticatorCategory(
                categoryID: binding.categoryID,
                authenticatorSecret: secret,
                ranking: binding.ranking
            )
        }
        customIcons = vault.customIcons.map(LegacyCustomIcon.init)
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        authenticators = try container.decode([LegacyAuthenticator].self, forKey: .authenticators)
        categories = try container.decodeIfPresent([LegacyCategory].self, forKey: .categories) ?? []
        authenticatorCategories = try container.decodeIfPresent(
            [LegacyAuthenticatorCategory].self,
            forKey: .authenticatorCategories
        ) ?? []
        customIcons = try container.decodeIfPresent([LegacyCustomIcon].self, forKey: .customIcons) ?? []
    }

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(authenticators, forKey: .authenticators)
        try container.encode(categories, forKey: .categories)
        try container.encode(authenticatorCategories, forKey: .authenticatorCategories)
        try container.encode(customIcons, forKey: .customIcons)
    }

    func makeVault() throws -> VaultData {
        let accounts = try authenticators.map { try $0.makeAuthenticator() }
        let accountIDs = Dictionary(accounts.map { ($0.secret, $0.id) }, uniquingKeysWith: { first, _ in first })
        let categoryIDs = Set(categories.map(\.id))
        let bindings = try authenticatorCategories.map { binding -> AuthenticatorCategory in
            guard let accountID = accountIDs[binding.authenticatorSecret],
                  categoryIDs.contains(binding.categoryID) else {
                throw StratumBackupError.invalidData("A backup category assignment refers to missing data.")
            }
            return AuthenticatorCategory(
                categoryID: binding.categoryID,
                authenticatorID: accountID,
                ranking: binding.ranking
            )
        }
        for account in accounts {
            try account.validate()
        }
        let restoredIcons = customIcons.map { $0.makeCustomIcon() }
        let referencedCustomIcons = Set(accounts.compactMap { auth -> String? in
            guard let icon = auth.icon, icon.hasPrefix("@") else { return nil }
            return String(icon.dropFirst())
        })
        guard referencedCustomIcons.isSubset(of: Set(restoredIcons.map(\.id))) else {
            throw StratumBackupError.invalidData("A backup account refers to a missing custom icon.")
        }
        return VaultData(
            authenticators: accounts,
            categories: categories.map { $0.makeCategory() },
            authenticatorCategories: bindings,
            customIcons: restoredIcons
        )
    }
}

private struct LegacyAuthenticator: Codable {
    var type: Int
    var icon: String?
    var issuer: String
    var username: String?
    var secret: String
    var pin: String?
    var algorithm: Int
    var digits: Int
    var period: Int
    var counter: Int64
    var ranking: Int
    var copyCount: Int

    enum CodingKeys: String, CodingKey {
        case type = "Type"
        case icon = "Icon"
        case issuer = "Issuer"
        case username = "Username"
        case secret = "Secret"
        case pin = "Pin"
        case algorithm = "Algorithm"
        case digits = "Digits"
        case period = "Period"
        case counter = "Counter"
        case ranking = "Ranking"
        case copyCount = "CopyCount"
    }

    init(_ authenticator: Authenticator) throws {
        type = try Self.legacyType(authenticator.type)
        icon = authenticator.icon
        issuer = authenticator.issuer
        username = authenticator.username
        secret = authenticator.secret
        pin = authenticator.pin
        algorithm = authenticator.algorithm == .sha1 ? 0 : authenticator.algorithm == .sha256 ? 1 : 2
        digits = authenticator.digits
        period = authenticator.period
        guard authenticator.counter <= UInt64(Int64.max) else {
            throw StratumBackupError.invalidData("An account counter exceeds the Stratum backup format limit.")
        }
        counter = Int64(authenticator.counter)
        ranking = authenticator.ranking
        copyCount = authenticator.copyCount
    }

    func makeAuthenticator() throws -> Authenticator {
        let type: OTPType
        switch self.type {
        case 1: type = .hotp
        case 2: type = .totp
        case 3: type = .mobileOtp
        case 4: type = .steam
        case 5: type = .yandex
        default: throw StratumBackupError.invalidData("The backup contains an unknown account type.")
        }
        let algorithm: OTPAlgorithm
        switch self.algorithm {
        case 0: algorithm = .sha1
        case 1: algorithm = .sha256
        case 2: algorithm = .sha512
        default: throw StratumBackupError.invalidData("The backup contains an unknown hashing algorithm.")
        }
        guard counter >= 0 else {
            throw StratumBackupError.invalidData("The backup contains a negative account counter.")
        }
        return Authenticator(
            type: type,
            issuer: issuer,
            username: username,
            secret: secret,
            pin: pin,
            algorithm: algorithm,
            digits: digits,
            period: period,
            counter: UInt64(counter),
            icon: icon,
            ranking: ranking,
            copyCount: copyCount
        )
    }

    private static func legacyType(_ type: OTPType) throws -> Int {
        return switch type {
        case .hotp: 1
        case .totp: 2
        case .mobileOtp: 3
        case .steam: 4
        case .yandex: 5
        }
    }
}

private struct LegacyCategory: Codable {
    var id: String
    var name: String
    var ranking: Int

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case name = "Name"
        case ranking = "Ranking"
    }

    init(_ category: AuthenticatorCategoryModel) {
        id = category.id
        name = category.name
        ranking = category.ranking
    }

    func makeCategory() -> AuthenticatorCategoryModel {
        AuthenticatorCategoryModel(id: id, name: name, ranking: ranking)
    }
}

private struct LegacyAuthenticatorCategory: Codable {
    var categoryID: String
    var authenticatorSecret: String
    var ranking: Int

    enum CodingKeys: String, CodingKey {
        case categoryID = "CategoryId"
        case authenticatorSecret = "AuthenticatorSecret"
        case ranking = "Ranking"
    }

    init(categoryID: String, authenticatorSecret: String, ranking: Int) {
        self.categoryID = categoryID
        self.authenticatorSecret = authenticatorSecret
        self.ranking = ranking
    }
}

private struct LegacyCustomIcon: Codable {
    var id: String
    var data: Data

    enum CodingKeys: String, CodingKey {
        case id = "Id"
        case data = "Data"
    }

    init(_ customIcon: CustomIcon) {
        id = customIcon.id
        data = customIcon.data
    }

    func makeCustomIcon() -> CustomIcon {
        CustomIcon(id: id, data: data)
    }
}
