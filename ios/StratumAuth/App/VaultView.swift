// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import StratumAuthCore
import SwiftUI
import UIKit
import UniformTypeIdentifiers

struct VaultView: View {
    @Environment(\.editMode) private var editMode
    @EnvironmentObject private var vault: VaultStore
    @AppStorage("appearance") private var appearance = AppearancePreference.system.rawValue
    @State private var editorRequest: EditorRequest?
    @State private var showingCategories = false
    @State private var showingExportOptions = false
    @State private var showingExporter = false
    @State private var showingImporter = false
    @State private var showingPasswordPrompt = false
    @State private var showingRestoreConfirmation = false
    @State private var readyToExport = false
    @State private var exportDocument = BackupDocument(data: Data())
    @State private var pendingBackupData: Data?
    @State private var pendingRestore: VaultData?
    @State private var backupPassword = ""
    @State private var selectedCategoryID: String?

    private var visibleAuthenticators: [Authenticator] {
        let sorted = vault.authenticators.sorted { $0.ranking < $1.ranking }
        guard let selectedCategoryID else { return sorted }
        let accountIDs = Set(vault.authenticatorCategories
            .filter { $0.categoryID == selectedCategoryID }
            .map(\.authenticatorID))
        return sorted.filter { accountIDs.contains($0.id) }
    }

    var body: some View {
        NavigationStack {
            Group {
                if visibleAuthenticators.isEmpty {
                    ContentUnavailableView(
                        selectedCategoryID == nil ? "No accounts yet" : "No accounts in this category",
                        systemImage: selectedCategoryID == nil ? "key.horizontal" : "folder",
                        description: Text(selectedCategoryID == nil
                            ? "Add an account by scanning its QR code or entering its details."
                            : "Assign accounts to this category while editing them.")
                    )
                } else {
                    List {
                        ForEach(visibleAuthenticators) { authenticator in
                            AccountRow(
                                authenticator: authenticator,
                                customIcon: vault.customIcon(for: authenticator)
                            ) {
                                vault.copyCode(for: authenticator)
                            }
                            .contextMenu {
                                Button("Edit", systemImage: "pencil") {
                                    editorRequest = EditorRequest(
                                        authenticator: authenticator,
                                        categoryIDs: vault.categoryIDs(for: authenticator)
                                    )
                                }
                                Button("Delete", systemImage: "trash", role: .destructive) {
                                    vault.delete(authenticator)
                                }
                            }
                        }
                        .onMove(perform: moveAuthenticators)
                        .onDelete(perform: deleteVisibleAccounts)
                    }
                    .listStyle(.insetGrouped)
                }
            }
            .navigationTitle(selectedCategoryName ?? "StratumAuth")
            .toolbar {
                ToolbarItem(placement: .topBarLeading) {
                    Menu {
                        Menu("Show accounts") {
                            Button("All accounts", systemImage: selectedCategoryID == nil ? "checkmark" : "folder") {
                                selectedCategoryID = nil
                            }
                            ForEach(vault.categories) { category in
                                Button(category.name, systemImage: selectedCategoryID == category.id ? "checkmark" : "folder") {
                                    selectedCategoryID = category.id
                                }
                            }
                        }
                        Divider()
                        Button("Categories", systemImage: "folder") {
                            showingCategories = true
                        }
                        Button("Import backup", systemImage: "square.and.arrow.down") {
                            showingImporter = true
                        }
                        Button("Export backup", systemImage: "square.and.arrow.up") {
                            showingExportOptions = true
                        }
                        Divider()
                        Picker("Appearance", selection: $appearance) {
                            ForEach(AppearancePreference.allCases) { option in
                                Text(option.title).tag(option.rawValue)
                            }
                        }
                    } label: {
                        Image(systemName: "line.3.horizontal.decrease")
                    }
                    .disabled(!vault.isLoaded)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    EditButton()
                        .disabled(!vault.isLoaded || selectedCategoryID != nil)
                }
                ToolbarItem(placement: .topBarTrailing) {
                    Button {
                        editorRequest = EditorRequest(authenticator: nil)
                    } label: {
                        Label("Add account", systemImage: "plus")
                    }
                    .disabled(!vault.isLoaded)
                }
            }
            .onChange(of: vault.categories) { _, categories in
                if let selectedCategoryID, !categories.contains(where: { $0.id == selectedCategoryID }) {
                    self.selectedCategoryID = nil
                }
            }
            .onChange(of: selectedCategoryID) { _, categoryID in
                if categoryID != nil {
                    editMode?.wrappedValue = .inactive
                }
            }
            .preferredColorScheme(AppearancePreference(rawValue: appearance)?.colorScheme)
            .sheet(item: $editorRequest) { request in
                if let authenticator = request.authenticator {
                    AuthenticatorEditor(
                        authenticator: authenticator,
                        categoryIDs: request.categoryIDs
                    ) { vault.update($0, categoryIDs: $1, customIcon: $2) }
                } else {
                    AuthenticatorEditor { vault.add($0, categoryIDs: $1, customIcon: $2) }
                }
            }
            .sheet(isPresented: $showingCategories) {
                CategoryManagerView()
            }
            .sheet(isPresented: $showingExportOptions, onDismiss: {
                if readyToExport {
                    readyToExport = false
                    showingExporter = true
                }
            }) {
                BackupExportOptions { password in
                    exportDocument = BackupDocument(data: try await vault.backupData(password: password))
                    readyToExport = true
                }
            }
            .fileExporter(
                isPresented: $showingExporter,
                document: exportDocument,
                contentType: .stratumBackup,
                defaultFilename: "StratumAuth-Backup"
            ) { result in
                if case .failure(let error) = result {
                    vault.errorMessage = "The backup could not be saved: \(error.localizedDescription)"
                }
            }
            .fileImporter(
                isPresented: $showingImporter,
                allowedContentTypes: [.data, .stratumBackup],
                allowsMultipleSelection: false,
                onCompletion: importFile
            )
            .alert("Enter backup password", isPresented: $showingPasswordPrompt) {
                SecureField("Password", text: $backupPassword)
                Button("Unlock", action: unlockBackup)
                Button("Cancel", role: .cancel) {
                    pendingBackupData = nil
                    backupPassword = ""
                }
            } message: {
                Text("Enter the password used to encrypt this Stratum backup.")
            }
            .confirmationDialog(
                "Replace all accounts with this backup?",
                isPresented: $showingRestoreConfirmation,
                titleVisibility: .visible
            ) {
                Button("Replace vault", role: .destructive) {
                    if let pendingRestore {
                        vault.restore(pendingRestore)
                    }
                    pendingRestore = nil
                }
                Button("Cancel", role: .cancel) {
                    pendingRestore = nil
                }
            } message: {
                Text("This replaces the current accounts, categories, and custom icons.")
            }
            .alert(
                "StratumAuth",
                isPresented: Binding(
                    get: { vault.errorMessage != nil },
                    set: { if !$0 { vault.errorMessage = nil } }
                )
            ) {
                Button("OK", role: .cancel) { vault.errorMessage = nil }
            } message: {
                Text(vault.errorMessage ?? "")
            }
        }
    }

    private var selectedCategoryName: String? {
        guard let selectedCategoryID else { return nil }
        return vault.categories.first { $0.id == selectedCategoryID }?.name
    }

    private func deleteVisibleAccounts(at offsets: IndexSet) {
        let accounts = visibleAuthenticators
        for index in offsets.sorted(by: >) {
            vault.delete(accounts[index])
        }
    }

    private func moveAuthenticators(from source: IndexSet, to destination: Int) {
        guard selectedCategoryID == nil else { return }
        var reordered = visibleAuthenticators
        reordered.move(fromOffsets: source, toOffset: destination)
        vault.reorderAuthenticators(reordered.map(\.id))
    }

    private func importFile(_ result: Result<[URL], Error>) {
        do {
            guard let url = try result.get().first else { return }
            let didAccess = url.startAccessingSecurityScopedResource()
            defer {
                if didAccess { url.stopAccessingSecurityScopedResource() }
            }
            let data = try Data(contentsOf: url)
            if data.starts(with: Data("AUTHENTICATORPRO".utf8))
                || data.starts(with: Data("AuthenticatorPro".utf8)) {
                pendingBackupData = data
                showingPasswordPrompt = true
            } else {
                Task { await restoreData(data, password: nil) }
            }
        } catch {
            vault.errorMessage = "The backup could not be opened: \(error.localizedDescription)"
        }
    }

    private func unlockBackup() {
        guard let data = pendingBackupData else { return }
        let password = backupPassword
        pendingBackupData = nil
        backupPassword = ""
        Task { await restoreData(data, password: password) }
    }

    private func restoreData(_ data: Data, password: String?) async {
        do {
            pendingRestore = try await vault.prepareRestore(data, password: password)
            showingRestoreConfirmation = true
        } catch {
            vault.errorMessage = error.localizedDescription
        }
    }
}

private enum AppearancePreference: String, CaseIterable, Identifiable {
    case system
    case light
    case dark

    var id: String { rawValue }

    var title: String {
        return switch self {
        case .system: "System"
        case .light: "Light"
        case .dark: "Dark"
        }
    }

    var colorScheme: ColorScheme? {
        return switch self {
        case .system: nil
        case .light: .light
        case .dark: .dark
        }
    }
}

private struct EditorRequest: Identifiable {
    let id = UUID()
    let authenticator: Authenticator?
    let categoryIDs: Set<String>

    init(authenticator: Authenticator?, categoryIDs: Set<String> = []) {
        self.authenticator = authenticator
        self.categoryIDs = categoryIDs
    }
}

private struct AccountRow: View {
    let authenticator: Authenticator
    let customIcon: CustomIcon?
    let onCopy: () -> Void

    var body: some View {
        TimelineView(.periodic(from: .now, by: 1)) { timeline in
            Button(action: onCopy) {
                HStack(spacing: 14) {
                    icon
                        .frame(width: 34, height: 34)
                    VStack(alignment: .leading, spacing: 5) {
                        Text(authenticator.issuer)
                            .font(.headline)
                            .foregroundStyle(.primary)
                        if let username = authenticator.username, !username.isEmpty {
                            Text(username)
                                .font(.subheadline)
                                .foregroundStyle(.secondary)
                        }
                        Text(code(at: timeline.date))
                            .font(.system(.title2, design: .monospaced, weight: .semibold))
                            .foregroundStyle(.primary)
                            .contentTransition(.numericText())
                    }
                    Spacer()
                    if authenticator.type != .hotp {
                        let remaining = remainingSeconds(at: timeline.date)
                        VStack(spacing: 4) {
                            ProgressView(
                                value: Double(remaining),
                                total: Double(authenticator.period)
                            )
                            .frame(width: 36)
                            Text("\(remaining)s")
                                .font(.caption2.monospacedDigit())
                                .foregroundStyle(.secondary)
                        }
                        .accessibilityLabel("\(remaining) seconds remaining")
                    }
                }
                .padding(.vertical, 5)
                .contentShape(Rectangle())
            }
            .buttonStyle(.plain)
            .accessibilityHint("Copies the one-time code")
        }
    }

    @ViewBuilder
    private var icon: some View {
        if let customIcon, let image = UIImage(data: customIcon.data) {
            Image(uiImage: image)
                .resizable()
                .scaledToFit()
                .clipShape(RoundedRectangle(cornerRadius: 7))
        } else {
            Image(systemName: symbolName)
                .font(.title2)
                .foregroundStyle(.tint)
        }
    }

    private var symbolName: String {
        return switch authenticator.type {
        case .totp: "timer"
        case .hotp: "number"
        case .mobileOtp: "lock"
        case .steam: "gamecontroller"
        case .yandex: "key"
        }
    }

    private func code(at date: Date) -> String {
        do {
            return try OTPGenerator.generate(for: authenticator, at: date)
        } catch {
            return "Invalid account"
        }
    }

    private func remainingSeconds(at date: Date) -> Int {
        let period = authenticator.type == .steam || authenticator.type == .yandex
            ? 30
            : authenticator.period
        let elapsed = Int64(date.timeIntervalSince1970) % Int64(period)
        return period - Int(elapsed)
    }
}
