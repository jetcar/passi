package com.passi.cloud.passi_android

import com.google.common.truth.Truth.assertThat
import com.passi.cloud.passi_android.domain.enrollment.CertificateGenerator
import com.passi.cloud.passi_android.domain.enrollment.GeneratedCertificate
import com.passi.cloud.passi_android.domain.enrollment.preparePendingDeviceKey
import com.passi.cloud.passi_android.domain.model.Account
import com.passi.cloud.passi_android.domain.repository.AccountsRepository
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.async
import kotlinx.coroutines.cancel
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.ensureActive
import kotlinx.coroutines.runBlocking
import org.junit.Test

class PendingKeyCheckpointFailureTests {
    @Test fun failedCheckpointDisposesUnreferencedKeyAndPreservesOriginalError() = runBlocking<Unit> {
        val repository = FailingCheckpointRepository()
        val account = repository.getAccounts().single()
        val generator = DisposableGenerator()
        val failure = runCatching { preparePendingDeviceKey(account, repository, generator) }.exceptionOrNull()
        assertThat(failure).isSameInstanceAs(repository.failure)
        assertThat(generator.disposed).containsExactly(
            sampleGeneratedCertificate().copy(
                keyProtection = com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection.BIOMETRIC_OR_DEVICE_CREDENTIAL,
            ),
        )
        assertThat(repository.getAccounts().single().pendingKey).isNull()
    }

    @Test fun disposalFailureIsSuppressedByOriginalCheckpointError() = runBlocking<Unit> {
        val repository = FailingCheckpointRepository()
        val account = repository.getAccounts().single()
        val generator = DisposableGenerator().apply { disposalFailure = IllegalStateException("Delete failed") }
        val failure = runCatching { preparePendingDeviceKey(account, repository, generator) }.exceptionOrNull()
        assertThat(failure).isSameInstanceAs(repository.failure)
        assertThat(failure!!.suppressed.toList()).containsExactly(generator.disposalFailure)
    }

    @Test fun unknownCheckpointOwnershipPreservesKey() = runBlocking<Unit> {
        val repository = FailingCheckpointRepository()
        val account = repository.getAccounts().single()
        repository.failReconciliation = true
        val generator = DisposableGenerator()
        val failure = runCatching { preparePendingDeviceKey(account, repository, generator) }.exceptionOrNull()
        assertThat(failure).isSameInstanceAs(repository.failure)
        assertThat(failure!!.suppressed.toList()).hasSize(1)
        assertThat(generator.disposed).isEmpty()
    }

    @Test fun checkpointAndDisposalErrorsPreserveTheOriginalThrowable() = runBlocking<Unit> {
        val repository = FailingCheckpointRepository().apply { failure = AssertionError("Storage error") }
        val account = repository.getAccounts().single()
        val generator = DisposableGenerator().apply { disposalFailure = AssertionError("Cleanup error") }
        val failure = runCatching { preparePendingDeviceKey(account, repository, generator) }.exceptionOrNull()
        assertThat(failure).isSameInstanceAs(repository.failure)
        assertThat(failure!!.suppressed.toList()).containsExactly(generator.disposalFailure)
        assertThat(generator.disposed).hasSize(1)
    }

    @Test fun committedThenCancelledCheckpointRetainsItsKeyAndReconcilesWithoutCancellation() = runBlocking<Unit> {
        val repository = FailingCheckpointRepository().apply { commitThenCancel = true }
        val account = repository.getAccounts().single()
        val generator = DisposableGenerator()
        val operation = async { preparePendingDeviceKey(account, repository, generator) }
        assertThat(runCatching { operation.await() }.exceptionOrNull()).isInstanceOf(CancellationException::class.java)
        assertThat(repository.reconciledAfterFailure).isTrue()
        assertThat(repository.getAccounts().single().pendingKey?.keyAlias).isEqualTo(sampleGeneratedCertificate().keyAlias)
        assertThat(generator.disposed).isEmpty()
    }

    private class DisposableGenerator : CertificateGenerator {
        val disposed = mutableListOf<GeneratedCertificate>()
        var disposalFailure: Throwable? = null
        override fun generate(
            email: String,
            protection: com.passi.cloud.passi_android.domain.enrollment.DeviceKeyProtection,
        ): GeneratedCertificate = sampleGeneratedCertificate().copy(keyProtection = protection)
        override fun validate(certificate: GeneratedCertificate) = Unit
        override fun dispose(certificate: GeneratedCertificate) {
            disposed += certificate
            disposalFailure?.let { throw it }
        }
    }

    private class FailingCheckpointRepository(
        private val delegate: AccountsRepository = inMemoryAccountsRepository(),
    ) : AccountsRepository by delegate {
        var failure: Throwable = IllegalStateException("Checkpoint commit failed")
        var failReconciliation = false
        var commitThenCancel = false
        var updateFailed = false
        var reconciledAfterFailure = false
        override suspend fun updateAccount(account: Account) {
            if (commitThenCancel) delegate.updateAccount(account)
            updateFailed = true
            if (commitThenCancel) {
                val cancellation = CancellationException("Cancelled after durable commit")
                currentCoroutineContext().cancel(cancellation)
                throw cancellation
            }
            throw failure
        }
        override suspend fun getAccounts(): List<Account> {
            currentCoroutineContext().ensureActive()
            if (updateFailed) {
                if (failReconciliation) error("Cannot determine checkpoint ownership")
                reconciledAfterFailure = true
            }
            return delegate.getAccounts()
        }
    }
}
