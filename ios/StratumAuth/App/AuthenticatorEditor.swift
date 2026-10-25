// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import StratumAuthCore
import UIKit
import SwiftUI
import PhotosUI

struct AuthenticatorEditor: View {
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject private var vault: VaultStore
    @State private var draft: Authenticator
    @State private var selectedCategoryIDs: Set<String>
    @State private var selectedPhoto: PhotosPickerItem?
    @State private var iconData: Data?
    @State private var isScanning = false
    @State private var errorMessage: String?

    private let isEditing: Bool
    private let onSave: (Authenticator, Set<String>, CustomIcon?) -> Void

    init(
        authenticator: Authenticator? = nil,
        categoryIDs: Set<String> = [],
        onSave: @escaping (Authenticator, Set<String>, CustomIcon?) -> Void
    ) {
        _draft = State(initialValue: authenticator ?? Authenticator(issuer: "", secret: ""))
        _selectedCategoryIDs = State(initialValue: categoryIDs)
        isEditing = authenticator != nil
        self.onSave = onSave
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Account") {
                    TextField("Issuer", text: $draft.issuer)
                        .textContentType(.organizationName)
                    TextField("Account name (optional)", text: optionalText($draft.username))
                        .textContentType(.username)
                    SecureField("Base32 secret", text: $draft.secret)
                        .textInputAutocapitalization(.characters)
                        .autocorrectionDisabled()
                    if draft.type == .mobileOtp {
                        SecureField("4-digit PIN", text: optionalText($draft.pin))
                            .keyboardType(.numberPad)
                    } else if draft.type == .yandex {
                        SecureField("PIN", text: optionalText($draft.pin))
                            .keyboardType(.numberPad)
                    }
                    Button {
                        isScanning = true
                    } label: {
                        Label("Scan QR code", systemImage: "qrcode.viewfinder")
                    }
                }

                Section("Code settings") {
                    Picker("Type", selection: $draft.type) {
                        ForEach(OTPType.allCases) { type in
                            Text(type.title).tag(type)
                        }
                    }
                    if draft.type == .totp || draft.type == .hotp {
                        Picker("Algorithm", selection: $draft.algorithm) {
                            ForEach(OTPAlgorithm.allCases) { algorithm in
                                Text(algorithm.rawValue).tag(algorithm)
                            }
                        }
                    }
                    if draft.type == .totp || draft.type == .hotp {
                        Stepper(
                            "\(draft.digits) digits",
                            value: $draft.digits,
                            in: draft.type == .hotp ? 6...8 : 6...10
                        )
                    } else {
                        LabeledContent("Code length", value: "\(draft.digits)")
                    }
                    if draft.type == .totp {
                        Stepper("Period: \(draft.period) seconds", value: $draft.period, in: 1...300)
                    } else if draft.type == .hotp {
                        Stepper("Counter: \(draft.counter)", value: $draft.counter, in: 0...UInt64.max)
                    }
                }

                Section("Categories") {
                    if vault.categories.isEmpty {
                        Text("Create categories from the vault menu.")
                            .foregroundStyle(.secondary)
                    } else {
                        ForEach(vault.categories) { category in
                            Toggle(category.name, isOn: categoryBinding(for: category.id))
                        }
                    }
                }

                Section("Custom icon") {
                    PhotosPicker(selection: $selectedPhoto, matching: .images) {
                        Label("Choose from Photos", systemImage: "photo")
                    }
                    if let iconData, let image = UIImage(data: iconData) {
                        Image(uiImage: image)
                            .resizable()
                            .scaledToFit()
                            .frame(maxHeight: 96)
                    } else if let icon = vault.customIcon(for: draft), let image = UIImage(data: icon.data) {
                        Image(uiImage: image)
                            .resizable()
                            .scaledToFit()
                            .frame(maxHeight: 96)
                    }
                }
            }
            .navigationTitle(isEditing ? "Edit account" : "Add account")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Save", action: save)
                        .disabled(draft.issuer.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                }
            }
            .sheet(isPresented: $isScanning) {
                QRScannerView(
                    isPresented: $isScanning,
                    onCode: importQR,
                    onError: { errorMessage = $0 }
                )
                .ignoresSafeArea()
            }
            .alert("Unable to add account", isPresented: Binding(
                get: { errorMessage != nil },
                set: { if !$0 { errorMessage = nil } }
            )) {
                Button("OK", role: .cancel) { errorMessage = nil }
            } message: {
                Text(errorMessage ?? "")
            }
            .onChange(of: draft.type) { _, type in
                draft.digits = type == .totp || type == .hotp
                    ? min(max(draft.digits, 6), type == .hotp ? 8 : 10)
                    : type.defaultDigits
                draft.algorithm = .sha1
                draft.period = 30
                draft.pin = nil
            }
            .onChange(of: selectedPhoto) { _, photo in
                guard let photo else { return }
                Task {
                    do {
                        guard let imageBytes = try await photo.loadTransferable(type: Data.self),
                              let image = UIImage(data: imageBytes) else {
                            throw ImageSelectionError.invalidImage
                        }
                        let originalSize = image.size
                        let longestSide = max(originalSize.width, originalSize.height)
                        guard longestSide > 0 else { throw ImageSelectionError.invalidImage }
                        let scale = min(128 / longestSide, 1)
                        let size = CGSize(width: originalSize.width * scale, height: originalSize.height * scale)
                        let renderer = UIGraphicsImageRenderer(size: size)
                        let compressed = renderer.pngData { _ in
                            image.draw(in: CGRect(origin: .zero, size: size))
                        }
                        guard compressed.count <= 128 * 1024 else {
                            throw ImageSelectionError.imageTooLarge
                        }
                        iconData = compressed
                    } catch {
                        errorMessage = "The selected image could not be loaded: \(error.localizedDescription)"
                    }
                }
            }
        }
    }

    private enum ImageSelectionError: LocalizedError {
        case invalidImage
        case imageTooLarge

        var errorDescription: String? {
            switch self {
            case .invalidImage:
                return "Choose a supported image."
            case .imageTooLarge:
                return "The selected image is too large to store as an account icon."
            }
        }
    }

    private func optionalText(_ value: Binding<String?>) -> Binding<String> {
        Binding(
            get: { value.wrappedValue ?? "" },
            set: { value.wrappedValue = $0.isEmpty ? nil : $0 }
        )
    }

    private func categoryBinding(for id: String) -> Binding<Bool> {
        Binding(
            get: { selectedCategoryIDs.contains(id) },
            set: { isSelected in
                if isSelected {
                    selectedCategoryIDs.insert(id)
                } else {
                    selectedCategoryIDs.remove(id)
                }
            }
        )
    }

    private func importQR(_ value: String) {
        do {
            let parsed = try OTPAuthURI.parse(value)
            draft = Authenticator(
                id: draft.id,
                type: parsed.type,
                issuer: parsed.issuer,
                username: parsed.username,
                secret: parsed.secret,
                pin: parsed.pin,
                algorithm: parsed.algorithm,
                digits: parsed.digits,
                period: parsed.period,
                counter: parsed.counter
            )
            errorMessage = nil
            isScanning = false
        } catch {
            errorMessage = error.localizedDescription
            isScanning = false
        }
    }

    private func save() {
        do {
            draft.issuer = draft.issuer.trimmingCharacters(in: .whitespacesAndNewlines)
            if draft.type != .mobileOtp {
                draft.secret = OTPGenerator.normaliseSecret(draft.secret)
            }
            try draft.validate()
            onSave(draft, selectedCategoryIDs, iconData.map(CustomIcon.init(data:)))
            dismiss()
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
