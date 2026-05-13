package com.passi.cloud.passi_android.domain.model

data class ApiPaths(
    val signup: String,
    val signupConfirmation: String,
    val signupCheck: String,
    val tokenUpdate: String,
    val cancelCheck: String,
    val authorize: String,
    val time: String,
    val updateCertificate: String,
    val checkForStartedSessions: String,
    val syncAccounts: String,
    val deleteAccount: String,
    val listDevices: String,
    val deleteDevice: String,
) {
    companion object {
        fun defaultPaths(): ApiPaths = ApiPaths(
            signup = "/api/SignUp/signup",
            signupConfirmation = "/api/SignUp/confirm",
            signupCheck = "/api/SignUp/check",
            tokenUpdate = "/api/Token/Update",
            cancelCheck = "/api/Auth/Cancel",
            authorize = "/api/Auth/Authorize",
            time = "/api/Service/Time",
            updateCertificate = "/api/Certificate/UpdatePublicCert",
            checkForStartedSessions = "/api/Auth/GetActiveSession",
            syncAccounts = "/api/Auth/SyncAccounts",
            deleteAccount = "/api/Auth/Delete",
            listDevices = "/api/Auth/Devices",
            deleteDevice = "/api/Auth/DeleteDevice",
        )
    }
}
