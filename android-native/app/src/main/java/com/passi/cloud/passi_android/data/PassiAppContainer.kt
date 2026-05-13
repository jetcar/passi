package com.passi.cloud.passi_android.data

import android.content.Context
import com.google.gson.Gson
import com.passi.cloud.passi_android.data.auth.BackendAuthSessionService
import com.passi.cloud.passi_android.data.auth.PendingSessionStore
import com.passi.cloud.passi_android.data.auth.Pkcs12AccountSigner
import com.passi.cloud.passi_android.data.account.BackendAccountManagementService
import com.passi.cloud.passi_android.data.biometric.LocalBiometricCertificateService
import com.passi.cloud.passi_android.data.certificate.BackendCertificateRotationService
import com.passi.cloud.passi_android.data.crypto.BouncyCastleCertificateGenerator
import com.passi.cloud.passi_android.data.local.PassiPreferences
import com.passi.cloud.passi_android.data.local.PendingEnrollmentStore
import com.passi.cloud.passi_android.data.notifications.BackendNotificationTokenRegistrationService
import com.passi.cloud.passi_android.data.notifications.NotificationOpenStore
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.repository.BackendEnrollmentService
import com.passi.cloud.passi_android.data.repository.PersistentAccountsRepository
import com.passi.cloud.passi_android.data.repository.PersistentProvidersRepository
import com.passi.cloud.passi_android.data.selection.SelectedAccountStore
import com.passi.cloud.passi_android.data.selection.SelectedProviderStore
import com.passi.cloud.passi_android.domain.account.AccountManagementService
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.certificate.CertificateRotationService
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.notifications.NotificationTokenRegistrationService
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import com.passi.cloud.passi_android.domain.service.EnrollmentService

interface PassiAppContainer {
    val accountsRepository: AccountsRepository
    val providersRepository: ProvidersRepository
    val enrollmentService: EnrollmentService
    val pendingEnrollmentStore: PendingEnrollmentStore
    val preferences: PassiPreferences
    val apiClient: PassiApiClient
    val certificateGenerator: CertificateGenerator
    val authSessionService: AuthSessionService
    val accountSigner: AccountSigner
    val pendingSessionStore: PendingSessionStore
    val selectedAccountStore: SelectedAccountStore
    val selectedProviderStore: SelectedProviderStore
    val accountManagementService: AccountManagementService
    val certificateRotationService: CertificateRotationService
    val biometricCertificateService: BiometricCertificateService
    val notificationOpenStore: NotificationOpenStore
    val notificationTokenRegistrationService: NotificationTokenRegistrationService
}

class DefaultPassiAppContainer(
    context: Context,
) : PassiAppContainer {
    override val preferences: PassiPreferences by lazy {
        PassiPreferences(context = context, gson = Gson())
    }

    override val apiClient: PassiApiClient by lazy {
        PassiApiClient(Gson())
    }

    override val certificateGenerator: CertificateGenerator by lazy {
        BouncyCastleCertificateGenerator()
    }

    override val accountSigner: AccountSigner by lazy {
        Pkcs12AccountSigner()
    }

    override val pendingSessionStore: PendingSessionStore by lazy {
        PendingSessionStore()
    }

    override val selectedAccountStore: SelectedAccountStore by lazy {
        SelectedAccountStore()
    }

    override val selectedProviderStore: SelectedProviderStore by lazy {
        SelectedProviderStore()
    }

    override val pendingEnrollmentStore: PendingEnrollmentStore by lazy {
        PendingEnrollmentStore()
    }

    override val notificationOpenStore: NotificationOpenStore by lazy {
        NotificationOpenStore()
    }

    override val biometricCertificateService: BiometricCertificateService by lazy {
        LocalBiometricCertificateService(preferences)
    }

    override val providersRepository: ProvidersRepository by lazy {
        PersistentProvidersRepository(preferences)
    }

    override val accountsRepository: AccountsRepository by lazy {
        PersistentAccountsRepository(preferences)
    }

    override val enrollmentService: EnrollmentService by lazy {
        BackendEnrollmentService(apiClient)
    }

    override val authSessionService: AuthSessionService by lazy {
        BackendAuthSessionService(
            apiClient = apiClient,
            accountsRepository = accountsRepository,
            providersRepository = providersRepository,
            signer = accountSigner,
            biometricCertificateService = biometricCertificateService,
            deviceIdProvider = { preferences.getOrCreateDeviceId() },
            gson = Gson(),
        )
    }

    override val accountManagementService: AccountManagementService by lazy {
        BackendAccountManagementService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            gson = Gson(),
        )
    }

    override val certificateRotationService: CertificateRotationService by lazy {
        BackendCertificateRotationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            certificateGenerator = certificateGenerator,
            accountSigner = accountSigner,
            biometricCertificateService = biometricCertificateService,
        )
    }

    override val notificationTokenRegistrationService: NotificationTokenRegistrationService by lazy {
        BackendNotificationTokenRegistrationService(
            apiClient = apiClient,
            providersRepository = providersRepository,
            deviceIdProvider = { preferences.getOrCreateDeviceId() },
        )
    }
}