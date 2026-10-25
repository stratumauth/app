// Copyright (C) 2026 StratumAuth contributors
// SPDX-License-Identifier: GPL-3.0-only

import SwiftUI
import VisionKit

struct QRScannerView: UIViewControllerRepresentable {
    @Binding var isPresented: Bool
    let onCode: (String) -> Void
    let onError: (String) -> Void

    func makeCoordinator() -> Coordinator {
        Coordinator(parent: self)
    }

    func makeUIViewController(context: Context) -> UIViewController {
        guard DataScannerViewController.isSupported, DataScannerViewController.isAvailable else {
            let controller = UIViewController()
            DispatchQueue.main.async {
                onError("QR scanning is not available on this device.")
                isPresented = false
            }
            return controller
        }

        let scanner = DataScannerViewController(
            recognizedDataTypes: [.barcode(symbologies: [.qr])],
            qualityLevel: .balanced,
            isHighlightingEnabled: true
        )
        scanner.delegate = context.coordinator
        do {
            try scanner.startScanning()
        } catch {
            DispatchQueue.main.async {
                onError(error.localizedDescription)
                isPresented = false
            }
        }
        return scanner
    }

    func updateUIViewController(_ uiViewController: UIViewController, context: Context) {}

    static func dismantleUIViewController(_ uiViewController: UIViewController, coordinator: Coordinator) {
        (uiViewController as? DataScannerViewController)?.stopScanning()
    }

    final class Coordinator: NSObject, DataScannerViewControllerDelegate {
        private let parent: QRScannerView
        private var didScan = false

        init(parent: QRScannerView) {
            self.parent = parent
        }

        func dataScanner(
            _ dataScanner: DataScannerViewController,
            didAdd addedItems: [RecognizedItem],
            allItems: [RecognizedItem]
        ) {
            guard !didScan else { return }
            for item in addedItems {
                if case .barcode(let barcode) = item, let payload = barcode.payloadStringValue {
                    didScan = true
                    DispatchQueue.main.async {
                        self.parent.onCode(payload)
                    }
                    return
                }
            }
        }
    }
}
