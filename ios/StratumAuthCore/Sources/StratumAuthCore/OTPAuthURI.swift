// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Foundation

public enum OTPAuthURI {
    public static func parse(_ uri: String) throws -> Authenticator {
        if uri.lowercased().hasPrefix("motp://") {
            return try parseMobileOTP(uri)
        }

        guard let components = URLComponents(string: uri),
              components.scheme?.lowercased() == "otpauth",
              let typeName = components.host?.lowercased() else {
            throw OTPError.unsupportedURI
        }

        var type: OTPType
        switch typeName {
        case "totp":
            type = .totp
        case "hotp":
            type = .hotp
        case "yaotp":
            type = .yandex
        default:
            throw OTPError.unsupportedURI
        }

        let rawLabel = String(components.percentEncodedPath.drop(while: { $0 == "/" }))
        guard let label = rawLabel.removingPercentEncoding, !label.isEmpty else {
            throw OTPError.invalidURI("The QR code does not contain an account name.")
        }
        let parameters = Dictionary(
            components.queryItems?.compactMap { item in
                item.value.map { (item.name.lowercased(), $0) }
            } ?? [],
            uniquingKeysWith: { first, _ in first }
        )
        guard let rawSecret = parameters["secret"], !rawSecret.isEmpty else {
            throw OTPError.invalidURI("The QR code does not contain a secret.")
        }

        var issuer: String
        var username: String?
        if let separator = label.firstIndex(of: ":") {
            let labelIssuer = String(label[..<separator]).trimmingCharacters(in: .whitespacesAndNewlines)
            let labelUsername = String(label[label.index(after: separator)...])
            if labelIssuer.isEmpty {
                issuer = labelUsername
                username = nil
            } else {
                issuer = labelIssuer
                username = labelUsername.isEmpty ? nil : labelUsername
            }
        } else if let queryIssuer = parameters["issuer"], !queryIssuer.isEmpty {
            issuer = queryIssuer
            username = label
        } else {
            issuer = label
            username = nil
        }

        if type == .yandex {
            issuer = "Yandex"
            username = label
        } else if type == .totp,
                  issuer == "Steam" || parameters["steam"] != nil || parameters["encoder"]?.lowercased() == "steam" {
            type = .steam
        }

        let algorithm: OTPAlgorithm
        if type == .totp || type == .hotp {
            switch parameters["algorithm"]?.uppercased() ?? "SHA1" {
            case "SHA1":
                algorithm = .sha1
            case "SHA256":
                algorithm = .sha256
            case "SHA512":
                algorithm = .sha512
            default:
                throw OTPError.invalidURI("The QR code uses an unsupported hashing algorithm.")
            }
        } else {
            algorithm = .sha1
        }

        let digits = type == .totp || type == .hotp
            ? try parseInteger(parameters["digits"], default: type.defaultDigits, label: "digits")
            : type.defaultDigits
        let period = type == .totp
            ? try parseInteger(parameters["period"], default: 30, label: "period")
            : 30
        let counterValue = type == .hotp ? try parseCounter(parameters["counter"]) : 0

        let authenticator = Authenticator(
            type: type,
            issuer: issuer,
            username: username,
            secret: rawSecret,
            algorithm: algorithm,
            digits: digits,
            period: period,
            counter: counterValue
        )
        try authenticator.validate(allowMissingPIN: type.needsPIN)
        return authenticator
    }

    private static func parseMobileOTP(_ uri: String) throws -> Authenticator {
        let components = uri.dropFirst("motp://".count)
        guard let queryStart = components.range(of: "?secret=") else {
            throw OTPError.invalidURI("The mOTP QR code is missing its secret.")
        }
        let label = String(components[..<queryStart.lowerBound]).removingPercentEncoding
            ?? String(components[..<queryStart.lowerBound])
        let secret = String(components[queryStart.upperBound...]).removingPercentEncoding
            ?? String(components[queryStart.upperBound...])
        guard let separator = label.firstIndex(of: ":"), !secret.isEmpty else {
            throw OTPError.invalidURI("The mOTP QR code must include an issuer, account name, and secret.")
        }
        let issuer = String(label[..<separator])
        let username = String(label[label.index(after: separator)...])
        let authenticator = Authenticator(
            type: .mobileOtp,
            issuer: issuer,
            username: username,
            secret: secret
        )
        try authenticator.validate(allowMissingPIN: true)
        return authenticator
    }

    private static func parseInteger(_ rawValue: String?, default defaultValue: Int, label: String) throws -> Int {
        guard let rawValue else {
            return defaultValue
        }
        guard let value = Int(rawValue) else {
            throw OTPError.invalidURI("The QR code has an invalid \(label) value.")
        }
        return value
    }

    private static func parseCounter(_ rawValue: String?) throws -> UInt64 {
        guard let rawValue else {
            return 0
        }
        guard let value = UInt64(rawValue) else {
            throw OTPError.invalidURI("The HOTP counter must be a non-negative integer.")
        }
        return value
    }
}
