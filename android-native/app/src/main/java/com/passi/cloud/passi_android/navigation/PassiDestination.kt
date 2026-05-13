package com.passi.cloud.passi_android.navigation

sealed interface PassiDestination {
    data object Accounts : PassiDestination
    data object AccountDetail : PassiDestination
    data object AccountDevices : PassiDestination
    data object UpdateCertificate : PassiDestination
    data object Providers : PassiDestination
    data object ProviderDetail : PassiDestination
    data object ProviderEditor : PassiDestination
    data object Terms : PassiDestination
    data object AddAccount : PassiDestination
    data object ConfirmCode : PassiDestination
    data object FinishEnrollment : PassiDestination
    data object SessionChallenge : PassiDestination
    data object SessionPin : PassiDestination
}