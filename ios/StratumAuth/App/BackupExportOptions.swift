// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import SwiftUI

struct BackupExportOptions: View {
    @Environment(\.dismiss) private var dismiss
    @State private var encryptBackup = true
    @State private var password = ""
    @State private var confirmation = ""
    @State private var errorMessage: String?
    @State private var isExporting = false

    let onExport: (String?) async throws -> Void

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    Toggle("Encrypt backup", isOn: $encryptBackup)
                    if encryptBackup {
                        SecureField("Password", text: $password)
                        SecureField("Confirm password", text: $confirmation)
                        Text("Encrypted Stratum backups use Argon2id and AES-GCM. Keep the password safe; it cannot be recovered.")
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    } else {
                        Label("Unencrypted backup contains all account secrets.", systemImage: "exclamationmark.triangle")
                            .font(.footnote)
                            .foregroundStyle(.orange)
                    }
                }
            }
            .navigationTitle("Export backup")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                        .disabled(isExporting)
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button(isExporting ? "Preparing…" : "Export", action: export)
                        .disabled(isExporting || !isValid)
                }
            }
            .alert("Unable to export backup", isPresented: Binding(
                get: { errorMessage != nil },
                set: { if !$0 { errorMessage = nil } }
            )) {
                Button("OK", role: .cancel) { errorMessage = nil }
            } message: {
                Text(errorMessage ?? "")
            }
        }
    }

    private var isValid: Bool {
        !encryptBackup || (!password.isEmpty && password == confirmation)
    }

    private func export() {
        isExporting = true
        Task {
            do {
                try await onExport(encryptBackup ? password : nil)
                dismiss()
            } catch {
                errorMessage = error.localizedDescription
            }
            isExporting = false
        }
    }
}
