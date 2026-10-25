// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import StratumAuthCore
import SwiftUI

struct CategoryManagerView: View {
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject private var vault: VaultStore
    @State private var categoryName = ""

    var body: some View {
        NavigationStack {
            List {
                Section {
                    HStack {
                        TextField("New category", text: $categoryName)
                            .textInputAutocapitalization(.words)
                        Button("Add", action: addCategory)
                            .disabled(categoryName.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                    }
                }
                Section("Categories") {
                    if vault.categories.isEmpty {
                        Text("No categories")
                            .foregroundStyle(.secondary)
                    }
                    ForEach(vault.categories) { category in
                        HStack {
                            Label(category.name, systemImage: "folder")
                            Spacer()
                            Text("\(vault.authenticatorCategories.filter { $0.categoryID == category.id }.count)")
                                .foregroundStyle(.secondary)
                        }
                        .swipeActions {
                            Button("Delete", systemImage: "trash", role: .destructive) {
                                vault.deleteCategory(category)
                            }
                        }
                    }
                }
            }
            .navigationTitle("Categories")
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .confirmationAction) {
                    Button("Done") { dismiss() }
                }
            }
            .alert(
                "Unable to manage categories",
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

    private func addCategory() {
        vault.addCategory(name: categoryName)
        if vault.errorMessage == nil {
            categoryName = ""
        }
    }
}
