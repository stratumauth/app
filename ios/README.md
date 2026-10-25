# StratumAuth for iOS

This is a native SwiftUI iOS app alongside the existing Android and Wear OS apps. It supports TOTP/HOTP with SHA-1/SHA-256/SHA-512, mOTP, Steam, and Yandex; Base32 `otpauth://` and mOTP QR codes; manual entry; live time-based countdowns; copying codes; account editing/deletion and reordering; categories; custom icons; and system/light/dark appearance choices.

Authenticator records, PINs, categories, and icons are stored together in a Keychain generic-password item with `WhenUnlockedThisDeviceOnly` accessibility. The app has no network or analytics integration. Camera access is requested only when scanning a QR code. Photos are selected with the system photo picker and reduced to a 128-pixel icon before storage.

Backup export/import uses Stratum's compatible JSON format, strong Argon2id + AES-GCM encryption, and legacy PBKDF2 + AES-CBC decryption. Unencrypted exports are supported for compatibility and explicitly warned as containing account secrets.

Open `StratumAuth/StratumAuth.xcodeproj` with Xcode 15.3 or newer and run the `StratumAuth` target on iOS 17 or newer. The portable core and protocol vectors can be tested on macOS with:

```sh
swift test --package-path ios/StratumAuthCore
```

Argon2id is provided by the MIT-licensed Swift package pinned to a specific revision; see `THIRD_PARTY_NOTICES.md`. Existing Android files and backup formats remain unchanged.
