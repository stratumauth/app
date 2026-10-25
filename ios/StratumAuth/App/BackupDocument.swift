// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import SwiftUI
import UniformTypeIdentifiers

extension UTType {
    static let stratumBackup = UTType(filenameExtension: "stratum", conformingTo: .data)!
}

struct BackupDocument: FileDocument {
    static var readableContentTypes: [UTType] { [.data, .stratumBackup] }
    static var writableContentTypes: [UTType] { [.stratumBackup] }

    var data: Data

    init(data: Data) {
        self.data = data
    }

    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents else {
            throw CocoaError(.fileReadCorruptFile)
        }
        self.data = data
    }

    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: data)
    }
}
