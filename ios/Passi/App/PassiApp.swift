import SwiftUI

@main
@MainActor
struct PassiApp: App {
    @StateObject private var store = PassiStore()

    var body: some Scene {
        WindowGroup {
            RootView()
                .environmentObject(store)
        }
    }
}
