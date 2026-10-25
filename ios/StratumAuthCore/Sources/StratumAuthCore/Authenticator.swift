// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Foundation

public enum OTPType: String, Codable, CaseIterable, Hashable, Identifiable, Sendable {
    case totp
    case hotp
    case mobileOtp
    case steam
    case yandex

    public var id: String { rawValue }

    public var defaultDigits: Int {
        return switch self {
        case .totp, .hotp, .mobileOtp: 6
        case .steam: 5
        case .yandex: 8
        }
    }

    public var needsPIN: Bool {
        self == .mobileOtp || self == .yandex
    }

    public var title: String {
        return switch self {
        case .totp: "TOTP"
        case .hotp: "HOTP"
        case .mobileOtp: "mOTP"
        case .steam: "Steam"
        case .yandex: "Yandex"
        }
    }
}

public enum OTPAlgorithm: String, Codable, CaseIterable, Hashable, Identifiable, Sendable {
    case sha1 = "SHA1"
    case sha256 = "SHA256"
    case sha512 = "SHA512"

    public var id: String { rawValue }
}

public struct Authenticator: Codable, Equatable, Identifiable, Sendable {
    public var id: UUID
    public var type: OTPType
    public var issuer: String
    public var username: String?
    public var secret: String
    public var pin: String?
    public var algorithm: OTPAlgorithm
    public var digits: Int
    public var period: Int
    public var counter: UInt64
    public var icon: String?
    public var ranking: Int
    public var copyCount: Int

    private enum CodingKeys: String, CodingKey {
        case id, type, issuer, username, secret, pin, algorithm, digits, period, counter, icon, ranking, copyCount
    }

    public init(
        id: UUID = UUID(),
        type: OTPType = .totp,
        issuer: String,
        username: String? = nil,
        secret: String,
        pin: String? = nil,
        algorithm: OTPAlgorithm = .sha1,
        digits: Int? = nil,
        period: Int = 30,
        counter: UInt64 = 0,
        icon: String? = nil,
        ranking: Int = 0,
        copyCount: Int = 0
    ) {
        self.id = id
        self.type = type
        self.issuer = issuer.trimmingCharacters(in: .whitespacesAndNewlines)
        self.username = username?.trimmingCharacters(in: .whitespacesAndNewlines)
        self.secret = type == .mobileOtp ? secret : OTPGenerator.normaliseSecret(secret)
        self.pin = pin
        self.algorithm = algorithm
        self.digits = digits ?? type.defaultDigits
        self.period = period
        self.counter = counter
        self.icon = icon
        self.ranking = ranking
        self.copyCount = copyCount
    }

    public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        id = try container.decodeIfPresent(UUID.self, forKey: .id) ?? UUID()
        type = try container.decodeIfPresent(OTPType.self, forKey: .type) ?? .totp
        issuer = try container.decode(String.self, forKey: .issuer)
        username = try container.decodeIfPresent(String.self, forKey: .username)
        let decodedSecret = try container.decode(String.self, forKey: .secret)
        secret = type == .mobileOtp ? decodedSecret : OTPGenerator.normaliseSecret(decodedSecret)
        pin = try container.decodeIfPresent(String.self, forKey: .pin)
        algorithm = try container.decodeIfPresent(OTPAlgorithm.self, forKey: .algorithm) ?? .sha1
        digits = try container.decodeIfPresent(Int.self, forKey: .digits) ?? type.defaultDigits
        period = try container.decodeIfPresent(Int.self, forKey: .period) ?? 30
        counter = try container.decodeIfPresent(UInt64.self, forKey: .counter) ?? 0
        icon = try container.decodeIfPresent(String.self, forKey: .icon)
        ranking = try container.decodeIfPresent(Int.self, forKey: .ranking) ?? 0
        copyCount = try container.decodeIfPresent(Int.self, forKey: .copyCount) ?? 0
    }

    public func validate(allowMissingPIN: Bool = false) throws {
        guard !issuer.isEmpty else {
            throw OTPError.invalidIssuer
        }
        let validDigits: Bool
        switch type {
        case .totp:
            validDigits = (6...10).contains(digits)
        case .hotp:
            validDigits = (6...8).contains(digits)
        case .mobileOtp, .steam, .yandex:
            validDigits = digits == type.defaultDigits
        }
        guard validDigits else {
            throw OTPError.invalidDigits
        }
        guard period > 0 else {
            throw OTPError.invalidPeriod
        }
        if type == .mobileOtp {
            guard secret.count >= 16 else { throw OTPError.invalidSecret }
            guard let pin else {
                if allowMissingPIN { return }
                throw OTPError.invalidPIN
            }
            guard pin.count == 4 else {
                throw OTPError.invalidPIN
            }
        } else {
            let secretBytes = try OTPGenerator.decodeBase32Secret(secret)
            if type == .yandex {
                guard secretBytes.count >= 16 else { throw OTPError.invalidSecret }
                guard let pin else {
                    if allowMissingPIN { return }
                    throw OTPError.invalidPIN
                }
                guard (4...16).contains(pin.count) else {
                    throw OTPError.invalidPIN
                }
            }
        }
    }
}
