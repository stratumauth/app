// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import SwiftUI

@main
struct StratumAuthApp: App {
    @StateObject private var vault = VaultStore()

    var body: some Scene {
        WindowGroup {
            VaultView()
                .environmentObject(vault)
        }
    }
}
