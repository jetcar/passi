package com.passi.cloud.passi_android.domain.enrollment

// Device credential with BiometricPrompt.CryptoObject requires Android 11 / API 30.
// https://developer.android.com/reference/androidx/biometric/BiometricPrompt
fun supportsDeviceKeys(apiLevel: Int): Boolean = apiLevel >= 30
