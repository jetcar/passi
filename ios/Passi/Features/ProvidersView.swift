import Foundation
import SwiftUI

struct ProvidersView: View {
    @EnvironmentObject private var store: PassiStore
    @State private var editingProvider: Provider?
    @State private var errorMessage: String?

    var body: some View {
        List {
            ForEach(store.providers) { provider in
                Button {
                    editingProvider = provider
                } label: {
                    VStack(alignment: .leading) {
                        HStack {
                            Text(provider.name).font(.headline)
                            if provider.isDefault { Text("Default").font(.caption).foregroundStyle(.secondary) }
                        }
                        Text(provider.baseURL).font(.caption).foregroundStyle(.secondary)
                    }
                }
                .buttonStyle(.plain)
            }
            .onDelete { indexes in
                for index in indexes {
                    do { try store.deleteProvider(store.providers[index]) }
                    catch { errorMessage = error.localizedDescription }
                }
            }
        }
        .navigationTitle("Providers")
        .toolbar {
            Button {
                editingProvider = Provider(
                    id: UUID(),
                    name: "",
                    baseURL: "https://",
                    apiPaths: .defaults,
                    isDefault: false
                )
            } label: {
                Label("Add provider", systemImage: "plus")
            }
        }
        .sheet(item: $editingProvider) { provider in
            ProviderEditorView(provider: provider) { updated in
                do {
                    try store.saveProvider(updated)
                    editingProvider = nil
                } catch { errorMessage = error.localizedDescription }
            }
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
}

private struct ProviderEditorView: View {
    @Environment(\.dismiss) private var dismiss
    @State private var provider: Provider
    let onSave: (Provider) -> Void

    init(provider: Provider, onSave: @escaping (Provider) -> Void) {
        _provider = State(initialValue: provider)
        self.onSave = onSave
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Provider") {
                    TextField("Name", text: $provider.name)
                    TextField("Base URL", text: $provider.baseURL)
                        .textInputAutocapitalization(.never)
                        .keyboardType(.URL)
                }
                Section("API paths") {
                    path("Signup", value: $provider.apiPaths.signup)
                    path("Confirm signup", value: $provider.apiPaths.signupConfirmation)
                    path("Check code", value: $provider.apiPaths.signupCheck)
                    path("Cancel login", value: $provider.apiPaths.cancelCheck)
                    path("Authorize", value: $provider.apiPaths.authorize)
                    path("Pending sessions", value: $provider.apiPaths.checkForStartedSessions)
                    path("Sync accounts", value: $provider.apiPaths.syncAccounts)
                    path("Delete account", value: $provider.apiPaths.deleteAccount)
                    path("Update certificate", value: $provider.apiPaths.updateCertificate)
                    path("Devices", value: $provider.apiPaths.listDevices)
                    path("Delete device", value: $provider.apiPaths.deleteDevice)
                    path("Token update", value: $provider.apiPaths.tokenUpdate)
                    path("Time", value: $provider.apiPaths.time)
                }
            }
            .navigationTitle(provider.isDefault ? "Default provider" : "Provider")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Cancel") { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") { onSave(provider) }
                        .disabled(provider.name.trimmingCharacters(in: .whitespaces).isEmpty)
                }
            }
        }
    }

    private func path(_ title: String, value: Binding<String>) -> some View {
        TextField(title, text: value).textInputAutocapitalization(.never)
    }
}
