// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import Combine
import Foundation
import StratumAuthCore
import UIKit

@MainActor
final class VaultStore: ObservableObject {
    @Published private(set) var vaultData = VaultData()
    @Published private(set) var isLoaded = false
    @Published var errorMessage: String?

    var authenticators: [Authenticator] { vaultData.authenticators }
    var categories: [AuthenticatorCategoryModel] { vaultData.categories.sorted { $0.ranking < $1.ranking } }
    var authenticatorCategories: [AuthenticatorCategory] { vaultData.authenticatorCategories }
    var customIcons: [CustomIcon] { vaultData.customIcons }

    init() {
        do {
            vaultData = try KeychainVault.load()
            isLoaded = true
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func add(_ authenticator: Authenticator, categoryIDs: Set<String>, customIcon: CustomIcon?) {
        var updated = vaultData
        var auth = authenticator
        let highestRanking = updated.authenticators.map(\.ranking).max() ?? -1
        if highestRanking == Int.max {
            updated.authenticators.sort { $0.ranking < $1.ranking }
            for index in updated.authenticators.indices {
                updated.authenticators[index].ranking = index
            }
            auth.ranking = updated.authenticators.count
        } else {
            auth.ranking = highestRanking + 1
        }
        if let customIcon {
            updated.customIcons.removeAll { $0.id == customIcon.id }
            updated.customIcons.append(customIcon)
            auth.icon = "@\(customIcon.id)"
        }
        updated.authenticators.append(auth)
        setCategoryBindings(for: auth, categories: categoryIDs, in: &updated)
        save(updated)
    }

    func update(
        _ authenticator: Authenticator,
        categoryIDs: Set<String>,
        customIcon: CustomIcon?
    ) {
        var updated = vaultData
        guard let index = updated.authenticators.firstIndex(where: { $0.id == authenticator.id }) else {
            errorMessage = "This account is no longer in the vault."
            return
        }
        var auth = authenticator
        if let customIcon {
            updated.customIcons.removeAll { $0.id == customIcon.id }
            updated.customIcons.append(customIcon)
            auth.icon = "@\(customIcon.id)"
        }
        updated.authenticators[index] = auth
        setCategoryBindings(for: auth, categories: categoryIDs, in: &updated)
        save(updated)
    }

    func delete(at offsets: IndexSet) {
        let ids = Set(offsets.map { vaultData.authenticators[$0].id })
        var updated = vaultData
        for index in offsets.sorted(by: >) {
            updated.authenticators.remove(at: index)
        }
        normalizeAuthenticatorRankings(in: &updated)
        updated.authenticatorCategories.removeAll { ids.contains($0.authenticatorID) }
        updated.customIcons = usedCustomIcons(in: updated)
        save(updated)
    }

    func delete(_ authenticator: Authenticator) {
        var updated = vaultData
        updated.authenticators.removeAll { $0.id == authenticator.id }
        normalizeAuthenticatorRankings(in: &updated)
        updated.authenticatorCategories.removeAll { $0.authenticatorID == authenticator.id }
        updated.customIcons = usedCustomIcons(in: updated)
        save(updated)
    }

    func reorderAuthenticators(_ orderedIDs: [UUID]) {
        guard orderedIDs.count == vaultData.authenticators.count,
              Set(orderedIDs).count == orderedIDs.count,
              Set(orderedIDs) == Set(vaultData.authenticators.map(\.id)) else {
            errorMessage = "The account order could not be updated."
            return
        }

        var updated = vaultData
        let rankingByID = Dictionary(uniqueKeysWithValues: orderedIDs.enumerated().map {
            ($0.element, $0.offset)
        })
        for index in updated.authenticators.indices {
            if let ranking = rankingByID[updated.authenticators[index].id] {
                updated.authenticators[index].ranking = ranking
            }
        }
        updated.authenticators.sort { $0.ranking < $1.ranking }
        save(updated)
    }

    func addCategory(name: String) {
        let category = AuthenticatorCategoryModel(name: name, ranking: vaultData.categories.count)
        guard !category.name.isEmpty else {
            errorMessage = "Enter a category name."
            return
        }
        guard !vaultData.categories.contains(where: { $0.name.localizedCaseInsensitiveCompare(category.name) == .orderedSame }) else {
            errorMessage = "A category with this name already exists."
            return
        }
        var updated = vaultData
        updated.categories.append(category)
        save(updated)
    }

    func deleteCategory(_ category: AuthenticatorCategoryModel) {
        var updated = vaultData
        updated.categories.removeAll { $0.id == category.id }
        updated.authenticatorCategories.removeAll { $0.categoryID == category.id }
        save(updated)
    }

    func categoryIDs(for authenticator: Authenticator) -> Set<String> {
        Set(vaultData.authenticatorCategories
            .filter { $0.authenticatorID == authenticator.id }
            .map(\.categoryID))
    }

    func customIcon(for authenticator: Authenticator) -> CustomIcon? {
        guard let icon = authenticator.icon, icon.hasPrefix("@") else { return nil }
        return vaultData.customIcons.first { $0.id == String(icon.dropFirst()) }
    }

    func restore(_ data: VaultData) {
        save(data)
    }

    func backupData(password: String?) async throws -> Data {
        let snapshot = vaultData
        return try await Task.detached(priority: .userInitiated) {
            try StratumBackup.export(snapshot, password: password)
        }.value
    }

    func prepareRestore(_ data: Data, password: String?) async throws -> VaultData {
        try await Task.detached(priority: .userInitiated) {
            try StratumBackup.import(data, password: password)
        }.value
    }

    func copyCode(for authenticator: Authenticator, at date: Date = .now) {
        do {
            var updated = vaultData
            guard let index = updated.authenticators.firstIndex(where: { $0.id == authenticator.id }) else {
                errorMessage = "This account is no longer in the vault."
                return
            }
            let currentAuthenticator = updated.authenticators[index]
            let code = try OTPGenerator.generate(for: currentAuthenticator, at: date)
            if currentAuthenticator.type == .hotp {
                guard updated.authenticators[index].counter < UInt64.max else {
                    errorMessage = "The HOTP counter reached its maximum value."
                    return
                }
                updated.authenticators[index].counter += 1
            }
            guard updated.authenticators[index].copyCount < Int.max else {
                errorMessage = "The account's copy count reached its maximum value."
                return
            }
            updated.authenticators[index].copyCount += 1
            try KeychainVault.save(updated)
            vaultData = updated
            UIPasteboard.general.string = code
            errorMessage = nil
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    private func normalizeAuthenticatorRankings(in data: inout VaultData) {
        data.authenticators.sort { $0.ranking < $1.ranking }
        for index in data.authenticators.indices {
            data.authenticators[index].ranking = index
        }
    }

    private func setCategoryBindings(
        for authenticator: Authenticator,
        categories: Set<String>,
        in data: inout VaultData
    ) {
        data.authenticatorCategories.removeAll { $0.authenticatorID == authenticator.id }
        for (ranking, categoryID) in categories.sorted().enumerated() {
            guard data.categories.contains(where: { $0.id == categoryID }) else { continue }
            data.authenticatorCategories.append(
                AuthenticatorCategory(categoryID: categoryID, authenticatorID: authenticator.id, ranking: ranking)
            )
        }
    }

    private func usedCustomIcons(in data: VaultData) -> [CustomIcon] {
        let ids = Set(data.authenticators.compactMap { auth -> String? in
            guard let icon = auth.icon, icon.hasPrefix("@") else { return nil }
            return String(icon.dropFirst())
        })
        return data.customIcons.filter { ids.contains($0.id) }
    }

    private func save(_ updated: VaultData) {
        guard isLoaded else {
            errorMessage = "The secure vault could not be opened. Restart the app before changing accounts."
            return
        }
        do {
            try KeychainVault.save(updated)
            vaultData = updated
            errorMessage = nil
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
