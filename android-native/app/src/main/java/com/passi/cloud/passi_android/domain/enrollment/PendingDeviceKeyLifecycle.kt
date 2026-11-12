package com.passi.cloud.passi_android.domain.enrollment

import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.withContext

class DeviceKeyRecoveryRequiredException : IllegalStateException(
    "Pending device key requires account recovery",
)

/** A possibly submitted certificate is immutable, even when its private key is no longer usable. */
suspend fun preparePendingDeviceKey(
    account: Account,
    repository: AccountsRepository,
    generator: CertificateGenerator,
    beforeGeneration: suspend () -> Unit = {},
): Account {
    var current = account
    current.pendingKey?.let { pending ->
        try {
            generator.validate(pending.certificate())
            return current
        } catch (cancelled: CancellationException) {
            throw cancelled
        } catch (_: Exception) {
            if (pending.mayHaveBeenSubmitted) throw DeviceKeyRecoveryRequiredException()
            repository.discardUnsubmittedPendingKey(current.id, pending.keyAlias)
            current = repository.getAccount(current.id) ?: error("Account not found")
        }
    }
    beforeGeneration()
    val pending = generator.generate(current.email, null).checkpoint()
    current = current.copy(pendingKey = pending)
    try {
        repository.updateAccount(current)
    } catch (failure: Throwable) {
        // A durable commit can succeed just before its dispatcher return throws cancellation.
        // Reconcile every account, including a concurrent canonical-ID replacement, before disposal.
        try {
            withContext(NonCancellable) {
                withContext(Dispatchers.IO) {
                    try {
                        val referenced = repository.getAccounts().any {
                            pending.keyAlias in (listOfNotNull(it.keyAlias, it.pendingKey?.keyAlias, it.cleanupKeyAlias) +
                                it.retiredKeyAliases.orEmpty())
                        }
                        if (!referenced) generator.dispose(pending.certificate())
                    } catch (cleanupFailure: Throwable) {
                        if (cleanupFailure !== failure) failure.addSuppressed(cleanupFailure)
                    }
                }
            }
        } catch (cleanupFailure: Throwable) {
            if (cleanupFailure !== failure) failure.addSuppressed(cleanupFailure)
        }
        throw failure
    }
    generator.validate(pending.certificate())
    return current
}
