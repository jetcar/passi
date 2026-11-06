import Foundation
import SwiftUI

struct SessionApprovalView: View {
    @EnvironmentObject private var store: PassiStore
    @Environment(\.dismiss) private var dismiss
    let session: NotificationSession

    @State private var colorChoices: [ConfirmationColor]
    @State private var numberChoices: [Int]
    @State private var challengeAccepted = false
    @State private var challengeLocked = false
    @State private var pin = ""
    @State private var isWorking = false
    @State private var errorMessage: String?

    init(session: NotificationSession) {
        self.session = session
        var random = SystemRandomNumberGenerator()
        _colorChoices = State(initialValue: ChallengeChoices.colors(correct: session.confirmationColor, random: &random))
        _numberChoices = State(initialValue: session.confirmationNumber.map {
            ChallengeChoices.numbers(correct: $0, random: &random)
        } ?? [])
    }

    private var account: Account? { store.accounts.first { $0.id == session.accountID } }

    var body: some View {
        NavigationStack {
            VStack(spacing: 24) {
                Image(systemName: "person.badge.key.fill")
                    .font(.system(size: 54))
                    .foregroundStyle(.blue)
                VStack(spacing: 5) {
                    Text(session.sender).font(.title2.bold())
                    Text(session.returnHost).foregroundStyle(.secondary)
                }

                if challengeLocked {
                    Label("That did not match the browser. Cancel this request and try again.", systemImage: "xmark.octagon.fill")
                        .foregroundStyle(.red)
                        .multilineTextAlignment(.center)
                } else if !challengeAccepted {
                    challenge
                } else {
                    approvalControls
                }

                Spacer()
                Button("Cancel login", role: .destructive, action: cancel)
                    .disabled(isWorking)
            }
            .padding(24)
            .navigationTitle("Approve login")
            .navigationBarTitleDisplayMode(.inline)
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

    @ViewBuilder
    private var challenge: some View {
        if let correctNumber = session.confirmationNumber {
            Text("Choose the number shown in your browser").font(.headline)
            HStack(spacing: 12) {
                ForEach(numberChoices, id: \.self) { number in
                    Button(String(format: "%02d", number)) {
                        answer(number == correctNumber)
                    }
                    .buttonStyle(.borderedProminent)
                    .font(.title2.monospacedDigit().bold())
                }
            }
        } else {
            Text("Choose the color shown in your browser").font(.headline)
            HStack(spacing: 18) {
                ForEach(colorChoices, id: \.self) { color in
                    Button { answer(color == session.confirmationColor) } label: {
                        Circle()
                            .fill(color.swiftUIColor)
                            .frame(width: 64, height: 64)
                            .overlay(Circle().stroke(.white, lineWidth: 3))
                            .shadow(radius: 3)
                    }
                    .accessibilityLabel(color.rawValue.capitalized)
                }
            }
        }
    }

    @ViewBuilder
    private var approvalControls: some View {
        if let account, account.pinLength > 0 {
            SecureField("PIN", text: Binding(
                get: { pin },
                set: { pin = String($0.filter(\.isNumber).prefix(account.pinLength)) }
            ))
            .keyboardType(.numberPad)
            .textFieldStyle(.roundedBorder)

            Button("Approve with PIN") { approve(useBiometrics: false) }
                .buttonStyle(.borderedProminent)
                .disabled(isWorking || pin.count != account.pinLength)

            if account.biometricsEnabled {
                Button("Approve with Face ID") { approve(useBiometrics: true) }
                    .buttonStyle(.bordered)
                    .disabled(isWorking)
            }
        } else if account?.biometricsEnabled == true {
            Button("Approve with Face ID") { approve(useBiometrics: true) }
                .buttonStyle(.borderedProminent)
                .disabled(isWorking)
        } else {
            Button("Approve") { approve(useBiometrics: false) }
                .buttonStyle(.borderedProminent)
                .disabled(isWorking)
        }
    }

    private func answer(_ correct: Bool) {
        challengeAccepted = correct
        challengeLocked = !correct
    }

    private func approve(useBiometrics: Bool) {
        Task {
            isWorking = true
            do {
                try await store.approve(session, pin: pin.isEmpty ? nil : pin, useBiometrics: useBiometrics)
                dismiss()
            } catch { errorMessage = error.localizedDescription }
            isWorking = false
        }
    }

    private func cancel() {
        Task {
            isWorking = true
            do { try await store.cancel(session) }
            catch { errorMessage = error.localizedDescription }
            isWorking = false
            if store.pendingSession == nil { dismiss() }
        }
    }
}

private extension ConfirmationColor {
    var swiftUIColor: Color {
        switch self {
        case .blue: .blue
        case .red: .red
        case .green: .green
        case .yellow: .yellow
        }
    }
}
