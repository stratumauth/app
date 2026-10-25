// swift-tools-version: 5.10
import PackageDescription

let package = Package(
    name: "StratumAuthCore",
    platforms: [.iOS(.v17), .macOS(.14)],
    products: [
        .library(name: "StratumAuthCore", targets: ["StratumAuthCore"])
    ],
    dependencies: [
        .package(
            url: "https://github.com/MarlonJD/argon2id-swift-native.git",
            revision: "14d47de1914ac63b368ddb2cfe0f47ffe25f04cf"
        )
    ],
    targets: [
        .target(
            name: "StratumAuthCore",
            dependencies: [.product(name: "Argon2idSwiftNative", package: "argon2id-swift-native")]
        ),
        .testTarget(name: "StratumAuthCoreTests", dependencies: ["StratumAuthCore"])
    ]
)
