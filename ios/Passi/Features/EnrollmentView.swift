import Foundation
import SwiftUI

struct EnrollmentView: View {
    @EnvironmentObject private var store: PassiStore
    @Environment(\.dismiss) private var dismiss

    @State private var stage: Stage = .terms
    @State private var acceptedTerms = false
    @State private var email = ""
    @State private var providerID = Provider.passi.id
    @State private var code = ""
    @State private var pin = ""
    @State private var pinConfirmation = ""
    @State private var enableFaceID = false
    @State private var isWorking = false
    @State private var errorMessage: String?

    enum Stage {
        case terms, details, code, security
    }

    var body: some View {
        Form {
            switch stage {
            case .terms: terms
            case .details: details
            case .code: confirmation
            case .security: security
            }
        }
        .navigationTitle("Add account")
        .navigationBarBackButtonHidden(isWorking)
        .onAppear(perform: resumeIfNeeded)
        .alert("Passi", isPresented: Binding(
            get: { errorMessage != nil },
            set: { if !$0 { errorMessage = nil } }
        )) {
            Button("OK") { errorMessage = nil }
        } message: {
            Text(errorMessage ?? "")
        }
    }

    private var terms: some View {
        Group {
            Section("Before you continue") {
                Text("Passi creates a private login key on this iPhone. The key stays in the iOS Keychain and is used only when you approve a login.")
                Toggle("I understand and want to continue", isOn: $acceptedTerms)
            }
            Section {
                Button("Continue") { stage = .details }
                    .disabled(!acceptedTerms)
            }
        }
    }

    private var details: some View {
        Group {
            Section("Account") {
                TextField("Email", text: $email)
                    .textContentType(.emailAddress)
                    .textInputAutocapitalization(.never)
                    .keyboardType(.emailAddress)
                Picker("Provider", selection: $providerID) {
                    ForEach(store.providers) { provider in
                        Text(provider.name).tag(provider.id)
                    }
                }
            }
            Section {
                Button("Send confirmation code", action: begin)
                    .disabled(isWorking || !email.contains("@"))
            }
        }
    }

    private var confirmation: some View {
        Group {
            Section("Check your email") {
                Text("Enter the confirmation code sent to \(store.pendingEnrollment?.email ?? email).")
                TextField("Confirmation code", text: $code)
                    .textContentType(.oneTimeCode)
                    .keyboardType(.numberPad)
            }
            Section {
                Button("Verify code", action: verifyCode)
                    .disabled(isWorking || code.isEmpty)
            }
        }
    }

    private var security: some View {
        Group {
            Section("Optional PIN") {
                SecureField("PIN (4–12 digits)", text: pinBinding)
                    .keyboardType(.numberPad)
                SecureField("Confirm PIN", text: pinConfirmationBinding)
                    .keyboardType(.numberPad)
                Text("Leave both fields empty to approve without a Passi PIN.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
            Section("Face ID") {
                Toggle("Enable Face ID approvals", isOn: $enableFaceID)
            }
            Section {
                Button("Finish enrollment", action: finish)
                    .disabled(isWorking || pin != pinConfirmation || (!pin.isEmpty && pin.count < 4))
            }
        }
    }

    private var pinBinding: Binding<String> {
        Binding(get: { pin }, set: { pin = String($0.filter(\.isNumber).prefix(12)) })
    }

    private var pinConfirmationBinding: Binding<String> {
        Binding(get: { pinConfirmation }, set: { pinConfirmation = String($0.filter(\.isNumber).prefix(12)) })
    }

    private func resumeIfNeeded() {
        guard let pending = store.pendingEnrollment else { return }
        email = pending.email
        providerID = pending.providerID
        stage = pending.confirmationCode == nil ? .code : .security
    }

    private func begin() {
        Task {
            isWorking = true
            do {
                try await store.beginEnrollment(email: email.trimmingCharacters(in: .whitespaces), providerID: providerID)
                stage = .code
            } catch { errorMessage = error.localizedDescription }
            isWorking = false
        }
    }

    private func verifyCode() {
        Task {
            isWorking = true
            do {
                try await store.confirmEnrollmentCode(code)
                stage = .security
            } catch { errorMessage = error.localizedDescription }
            isWorking = false
        }
    }

    private func finish() {
        Task {
            isWorking = true
            do {
                try await store.finishEnrollment(pin: pin, enableBiometrics: enableFaceID)
                dismiss()
            } catch { errorMessage = error.localizedDescription }
            isWorking = false
        }
    }
}
