package com.passi.cloud.passi_android.data.auth

import com.google.gson.FieldNamingPolicy
import com.google.gson.Gson
import com.google.gson.GsonBuilder
import com.google.gson.reflect.TypeToken
import com.passi.cloud.passi_android.data.remote.PassiApiClient
import com.passi.cloud.passi_android.data.remote.dto.AccountMinDto
import com.passi.cloud.passi_android.data.remote.dto.AuthorizeRequestDto
import com.passi.cloud.passi_android.data.remote.dto.GetAllSessionRequestDto
import com.passi.cloud.passi_android.data.remote.dto.NotificationDto
import com.passi.cloud.passi_android.data.remote.dto.SyncAccountsRequestDto
import com.passi.cloud.passi_android.domain.auth.AccountSigner
import com.passi.cloud.passi_android.domain.auth.AuthSessionService
import com.passi.cloud.passi_android.domain.biometric.BiometricCertificateService
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.model.ConfirmationColor
import com.passi.cloud.passi_android.domain.model.NotificationSession
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import com.passi.cloud.passi_android.domain.repository.ProvidersRepository
import java.time.Instant
import java.util.UUID

class BackendAuthSessionService(
    private val apiClient: PassiApiClient,
    private val accountsRepository: AccountsRepository,
    private val providersRepository: ProvidersRepository,
    private val signer: AccountSigner,
    private val biometricCertificateService: BiometricCertificateService,
    private val deviceIdProvider: () -> String,
    private val gson: Gson = GsonBuilder()
        .setFieldNamingPolicy(FieldNamingPolicy.UPPER_CAMEL_CASE)
        .disableHtmlEscaping()
        .create(),
) : AuthSessionService {
    override suspend fun syncAndPollPendingSession(): Result<NotificationSession?> {
        val accounts = accountsRepository.getAccounts()
        if (accounts.isEmpty()) {
            return Result.success(null)
        }

        val providers = providersRepository.getProviders()
        val deviceId = deviceIdProvider()

        accounts.groupBy { it.providerId }.forEach { (providerId, groupedAccounts) ->
            val provider = providers.firstOrNull { it.id == providerId } ?: providers.firstOrNull { it.isDefault }
                ?: return@forEach

            val syncResult = apiClient.postJson(
                baseUrl = provider.baseUrl,
                path = provider.apiPaths.syncAccounts,
                payload = SyncAccountsRequestDto(
                    deviceId = deviceId,
                    guids = accounts.map { it.id.toString() },
                )
            )

            if (syncResult.isSuccessful) {
                val type = object : TypeToken<List<AccountMinDto>>() {}.type
                val serverAccounts = runCatching {
                    gson.fromJson<List<AccountMinDto>>(syncResult.body, type) ?: emptyList()
                }.getOrDefault(emptyList())

                groupedAccounts.forEach { account ->
                    val existsOnServer = serverAccounts.any { it.userGuid == account.id.toString() }
                    // Do not auto-deactivate on missing sync entries because cross-device registrations
                    // can temporarily or permanently hide an account in this response.
                    if (existsOnServer && account.isConfirmed && account.inactive) {
                        accountsRepository.updateAccount(account.copy(inactive = false))
                    }
                }
            }

            val pendingSessionResult = apiClient.postJson(
                baseUrl = provider.baseUrl,
                path = provider.apiPaths.checkForStartedSessions,
                payload = GetAllSessionRequestDto(deviceId = deviceId),
            )

            if (pendingSessionResult.isSuccessful && pendingSessionResult.body.isNotBlank() && pendingSessionResult.body != "null") {
                val sessionDto = runCatching {
                    gson.fromJson(pendingSessionResult.body, NotificationDto::class.java)
                }.getOrNull()

                if (sessionDto != null) {
                    val session = runCatching { sessionDto.toDomain() }
                        .getOrElse { error ->
                            return Result.failure(
                                IllegalStateException(
                                    "Failed to parse pending session response: ${error.message.orEmpty()}",
                                )
                            )
                        }
                    return Result.success(session)
                }
            }
        }

        return Result.success(null)
    }

    override suspend fun authorize(session: NotificationSession, pin: String?): Result<Unit> {
        val account = accountsRepository.getAccount(session.accountId)
            ?: return Result.failure(IllegalStateException("Account not found"))
        val signedHash = signer.sign(account, pin, session.randomString)
            .getOrElse { return Result.failure(IllegalStateException("Invalid Pin")) }
        return authorizeWithSignature(session, account, signedHash)
    }

    override suspend fun authorizeWithBiometric(session: NotificationSession): Result<Unit> {
        val account = accountsRepository.getAccount(session.accountId)
            ?: return Result.failure(IllegalStateException("Account not found"))
        val signedHash = biometricCertificateService.sign(account, session.randomString)
            .getOrElse { error ->
                return Result.failure(IllegalStateException(error.message ?: "Biometric signing failed"))
            }
        return authorizeWithSignature(session, account, signedHash)
    }

    private suspend fun authorizeWithSignature(
        session: NotificationSession,
        account: Account,
        signedHash: String,
    ): Result<Unit> {
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))

        val result = apiClient.postJson(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.authorize,
            payload = AuthorizeRequestDto(
                signedHash = signedHash,
                publicCertThumbprint = account.thumbprint.orEmpty(),
                sessionId = session.sessionId.toString(),
            )
        )

        if (result.isSuccessful) {
            return Result.success(Unit)
        }

        val errorMessage = apiClient.extractErrorMessage(result.body)
        return Result.failure(IllegalStateException(errorMessage ?: "Network error. Try again"))
    }

    override suspend fun cancel(session: NotificationSession): Result<Unit> {
        val account = accountsRepository.getAccount(session.accountId)
            ?: return Result.failure(IllegalStateException("Account not found"))
        val provider = providersRepository.getProvider(account.providerId)
            ?: return Result.failure(IllegalStateException("Provider not found"))
        val result = apiClient.get(
            baseUrl = provider.baseUrl,
            path = provider.apiPaths.cancelCheck + "?SessionId=" + session.sessionId,
        )

        return if (result.isSuccessful) {
            Result.success(Unit)
        } else {
            Result.failure(IllegalStateException(apiClient.extractErrorMessage(result.body) ?: "Network error. Try again"))
        }
    }
}

private fun NotificationDto.toDomain(): NotificationSession = NotificationSession(
    sender = sender,
    confirmationColor = confirmationColor.toDomainConfirmationColor(),
    sessionId = UUID.fromString(sessionId),
    expirationTime = Instant.parse(expirationTime),
    randomString = randomString,
    returnHost = returnHost,
    accountId = UUID.fromString(accountGuid),
)

private fun Any?.toDomainConfirmationColor(): ConfirmationColor {
    val rawValue = this ?: throw IllegalStateException("Missing confirmationColor")

    if (rawValue is Number) {
        return when (rawValue.toInt()) {
            1 -> ConfirmationColor.BLUE
            2 -> ConfirmationColor.RED
            3 -> ConfirmationColor.GREEN
            4 -> ConfirmationColor.YELLOW
            else -> throw IllegalStateException("Unknown confirmationColor numeric value: ${rawValue.toInt()}")
        }
    }

    if (rawValue is String) {
        return when (rawValue.trim().lowercase()) {
            "blue", "1" -> ConfirmationColor.BLUE
            "red", "2" -> ConfirmationColor.RED
            "green", "3" -> ConfirmationColor.GREEN
            "yellow", "4" -> ConfirmationColor.YELLOW
            else -> throw IllegalStateException("Unknown confirmationColor string value: $rawValue")
        }
    }

    throw IllegalStateException("Unsupported confirmationColor type: ${rawValue::class.java.simpleName}")
}