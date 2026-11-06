import Foundation
import SwiftUI

private enum AppRoute: Hashable {
    case enrollment
    case providers
    case account(UUID)
}

struct RootView: View {
    @EnvironmentObject private var store: PassiStore
    @Environment(\.scenePhase) private var scenePhase
    @State private var path: [AppRoute] = []

    var body: some View {
        NavigationStack(path: $path) {
            List {
                Section {
                    Button {
                        Task { await store.poll(surfaceErrors: true) }
                    } label: {
                        HStack {
                            Label(store.isBusy ? "Checking server…" : "Refresh", systemImage: "arrow.clockwise")
                            Spacer()
                            if store.isBusy { ProgressView() }
                        }
                    }
                    .disabled(store.isBusy)

                    Text(pollStatus)
                        .font(.caption)
                        .foregroundStyle(store.lastPollError == nil ? Color.secondary : Color.red)
                }

                if store.accounts.isEmpty {
                    VStack(spacing: 10) {
                        Image(systemName: "person.badge.key").font(.largeTitle).foregroundStyle(.secondary)
                        Text("No accounts").font(.headline)
                        Text("Add your Passi account to approve logins on this iPhone.")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.center)
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 36)
                } else {
                    ForEach(store.accounts.sorted { $0.email < $1.email }) { account in
                        NavigationLink(value: account.isConfirmed ? AppRoute.account(account.id) : .enrollment) {
                            AccountRow(account: account, provider: store.provider(id: account.providerID))
                        }
                    }
                }
            }
            .navigationTitle("Passi")
            .toolbar {
                ToolbarItem(placement: .navigationBarLeading) {
                    NavigationLink(value: AppRoute.providers) {
                        Label("Providers", systemImage: "server.rack")
                    }
                }
                ToolbarItemGroup(placement: .navigationBarTrailing) {
                    NavigationLink(value: AppRoute.enrollment) {
                        Label("Add account", systemImage: "plus")
                    }
                }
            }
            .safeAreaInset(edge: .bottom) {
                Text("Version \(AppMetadata.version)")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .padding(.vertical, 6)
            }
            .refreshable {
                await store.poll(surfaceErrors: true)
            }
            .navigationDestination(for: AppRoute.self) { route in
                switch route {
                case .enrollment:
                    EnrollmentView()
                case .providers:
                    ProvidersView()
                case .account(let id):
                    AccountDetailView(accountID: id)
                }
            }
        }
        .sheet(item: $store.pendingSession) { session in
            SessionApprovalView(session: session)
                .environmentObject(store)
                .interactiveDismissDisabled()
        }
        .alert("Passi", isPresented: errorBinding) {
            Button("OK") { store.errorMessage = nil }
        } message: {
            Text(store.errorMessage ?? "")
        }
        .task(id: scenePhase) {
            guard scenePhase == .active else { return }
            while !Task.isCancelled && scenePhase == .active {
                await store.poll()
                do {
                    try await Task.sleep(nanoseconds: 5_000_000_000)
                } catch {
                    return
                }
            }
        }
    }

    private var pollStatus: String {
        if store.isBusy { return "Asking the server for pending login requests…" }
        if let error = store.lastPollError { return error }
        if let lastAttempt = store.lastPollAttemptAt {
            return "Server checked at \(lastAttempt.formatted(date: .omitted, time: .standard))."
        }
        return "The server is checked every five seconds while Passi is open."
    }

    private var errorBinding: Binding<Bool> {
        Binding(
            get: { store.errorMessage != nil },
            set: { if !$0 { store.errorMessage = nil } }
        )
    }
}

private struct AccountRow: View {
    let account: Account
    let provider: Provider?

    var body: some View {
        HStack(spacing: 12) {
            Image(systemName: account.isConfirmed ? "checkmark.shield.fill" : "clock.badge.exclamationmark")
                .font(.title2)
                .foregroundStyle(account.isConfirmed ? .green : .orange)
            VStack(alignment: .leading, spacing: 3) {
                Text(account.email).font(.headline)
                Text(account.isConfirmed ? (provider?.name ?? "Provider") : "Finish enrollment")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            Spacer()
            if account.biometricsEnabled {
                Image(systemName: "faceid").foregroundStyle(.blue)
            }
        }
        .padding(.vertical, 4)
    }
}

private struct AccountDetailView: View {
    @EnvironmentObject private var store: PassiStore
    @Environment(\.dismiss) private var dismiss
    let accountID: UUID

    @State private var pin = ""
    @State private var errorMessage: String?
    @State private var isWorking = false
    @State private var confirmDelete = false

    private var account: Account? { store.accounts.first { $0.id == accountID } }

    var body: some View {
        Form {
            if let account {
                Section("Account") {
                    LabeledContent("Email", value: account.email)
                    LabeledContent("Provider", value: store.provider(id: account.providerID)?.name ?? "Unknown")
                    LabeledContent("Certificate", value: account.thumbprint ?? "Not available")
                    LabeledContent("Valid until", value: account.validTo.formatted(date: .abbreviated, time: .omitted))
                }

                Section("Security") {
                    if account.pinLength > 0 {
                        SecureField("Current PIN", text: $pin)
                            .keyboardType(.numberPad)
                    }
                    Button(account.biometricsEnabled ? "Disable Face ID" : "Enable Face ID") {
                        updateBiometrics(!account.biometricsEnabled, account: account)
                    }
                    .disabled(isWorking)
                }

                Section {
                    Button("Delete account", role: .destructive) { confirmDelete = true }
                        .disabled(isWorking)
                }
            } else {
                Text("Account not found")
            }
        }
        .navigationTitle("Account")
        .alert("Delete account?", isPresented: $confirmDelete) {
            Button("Delete", role: .destructive) {
                guard let account else { return }
                Task {
                    isWorking = true
                    do {
                        try await store.deleteAccount(account)
                        dismiss()
                    } catch { errorMessage = error.localizedDescription }
                    isWorking = false
                }
            }
            Button("Cancel", role: .cancel) {}
        } message: {
            Text("This removes the account from Passi and this iPhone.")
        }
        .alert("Passi", isPresented: Binding(
            get: { errorMessage != nil },
            set: { if !$0 { errorMessage = nil } }
        )) {
            Button("OK") { errorMessage = nil }
        } message: {
            Text(errorMessage ?? "")
        }
    }

    private func updateBiometrics(_ enabled: Bool, account: Account) {
        Task {
            isWorking = true
            do {
                try await store.setBiometrics(enabled, for: account, pin: pin.isEmpty ? nil : pin)
                pin = ""
            } catch { errorMessage = error.localizedDescription }
            isWorking = false
        }
    }
}
