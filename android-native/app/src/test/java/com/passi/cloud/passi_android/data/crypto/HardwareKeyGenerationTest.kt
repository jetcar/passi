package com.passi.cloud.passi_android.data.crypto

import org.junit.Assert.assertEquals
import org.junit.Assert.assertSame
import org.junit.Assert.assertTrue
import org.junit.Test
import java.security.GeneralSecurityException
import java.security.ProviderException

class HardwareKeyGenerationTest {
    @Test fun acceptsBothPlatformRepresentationsOfPerUseAuthentication() {
        assertTrue(isPerUseAuthenticationDuration(0))
        assertTrue(isPerUseAuthenticationDuration(-1))
        assertEquals(false, isPerUseAuthenticationDuration(30))
    }

    @Test fun expectedStrongBoxFailuresCleanBeforeTeeFallback() {
        for (failure in listOf(GeneralSecurityException(), ProviderException())) {
            val events = mutableListOf<String>()
            val result = generateWithHardwareFallback(true, { strongBox ->
                events += if (strongBox) "strongbox" else "tee"
                if (strongBox) throw failure
                "certificate"
            }, { events += "cleanup" })
            assertEquals("certificate", result)
            assertEquals(listOf("strongbox", "cleanup", "tee"), events)
        }
    }

    @Test fun unexpectedStrongBoxFailuresAlwaysCleanAndNeverFallback() {
        for (failure in listOf(IllegalStateException(), AssertionError())) {
            val events = mutableListOf<String>()
            val result = runCatching {
                generateWithHardwareFallback(true, { strongBox ->
                    events += if (strongBox) "strongbox" else "tee"
                    throw failure
                }, { events += "cleanup" })
            }
            assertSame(failure, result.exceptionOrNull())
            assertEquals(listOf("strongbox", "cleanup"), events)
        }
    }

    @Test fun failedTeeFallbackAlsoCleansItsEntry() {
        val events = mutableListOf<String>()
        val finalFailure = IllegalStateException()
        val result = runCatching {
            generateWithHardwareFallback(true, { strongBox ->
                events += if (strongBox) "strongbox" else "tee"
                if (strongBox) throw ProviderException()
                throw finalFailure
            }, { events += "cleanup" })
        }
        assertSame(finalFailure, result.exceptionOrNull())
        assertEquals(listOf("strongbox", "cleanup", "tee", "cleanup"), events)
    }

    @Test fun cleanupFailurePreventsFallbackAndPreservesOriginalFailure() {
        val original = GeneralSecurityException()
        val cleanup = IllegalStateException()
        var attempts = 0
        val result = runCatching {
            generateWithHardwareFallback(true, { attempts++; throw original }, { throw cleanup })
        }
        assertEquals(1, attempts)
        assertSame(original, result.exceptionOrNull())
        assertSame(cleanup, original.suppressed.single())
    }

    @Test fun successfulStrongBoxKeyIsRetainedWithoutTeeAttempt() {
        val result = generateWithHardwareFallback(true, { strongBox ->
            assertTrue(strongBox)
            "certificate"
        }, { error("Successful key must remain") })
        assertEquals("certificate", result)
    }

    @Test fun devicesWithoutStrongBoxUseTeeAndCleanUnexpectedFailures() {
        var cleanups = 0
        val failure = AssertionError()
        val result = runCatching {
            generateWithHardwareFallback(false, { strongBox ->
                assertEquals(false, strongBox)
                throw failure
            }, { cleanups++ })
        }
        assertSame(failure, result.exceptionOrNull())
        assertEquals(1, cleanups)
    }
}
