package com.passi.cloud.passi_android.navigation

import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.platform.LocalContext
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.passi.cloud.passi_android.PassiApplication
import com.passi.cloud.passi_android.feature.account.AccountDevicesRoute
import com.passi.cloud.passi_android.feature.account.AccountDetailRoute
import com.passi.cloud.passi_android.feature.accounts.AccountsRoute
import com.passi.cloud.passi_android.feature.auth.SessionChallengeRoute
import com.passi.cloud.passi_android.feature.auth.SessionPinRoute
import com.passi.cloud.passi_android.feature.certificate.UpdateCertificateRoute
import com.passi.cloud.passi_android.feature.enrollment.AddAccountRoute
import com.passi.cloud.passi_android.feature.enrollment.ConfirmCodeRoute
import com.passi.cloud.passi_android.feature.enrollment.FinishEnrollmentRoute
import com.passi.cloud.passi_android.feature.enrollment.TermsRoute
import com.passi.cloud.passi_android.feature.providers.ProviderDetailRoute
import com.passi.cloud.passi_android.feature.providers.ProviderEditorRoute
import com.passi.cloud.passi_android.feature.providers.ProvidersRoute

private const val AccountsErrorKey = "accounts_error"

@Composable
fun PassiNavGraph() {
    val application = LocalContext.current.applicationContext as PassiApplication
    val navController = rememberNavController()
    val notificationOpenRequest by application.container.notificationOpenStore.openRequests.collectAsState()

    LaunchedEffect(notificationOpenRequest) {
        if (notificationOpenRequest > 0L) {
            navController.navigate(PassiDestination.Accounts.javaClass.simpleName) {
                launchSingleTop = true
                popUpTo(PassiDestination.Accounts.javaClass.simpleName) {
                    inclusive = false
                }
            }
        }
    }

    NavHost(
        navController = navController,
        startDestination = PassiDestination.Accounts.javaClass.simpleName
    ) {
        composable(PassiDestination.Accounts.javaClass.simpleName) { backStackEntry ->
            val externalErrorMessage = backStackEntry.savedStateHandle.get<String>(AccountsErrorKey)
            AccountsRoute(
                onAddAccount = {
                    navController.navigate(PassiDestination.Terms.javaClass.simpleName)
                },
                onOpenPendingSession = {
                    if (navController.currentDestination?.route != PassiDestination.SessionChallenge.javaClass.simpleName) {
                        navController.navigate(PassiDestination.SessionChallenge.javaClass.simpleName) {
                            launchSingleTop = true
                        }
                    }
                },
                onOpenAccount = {
                    navController.navigate(PassiDestination.AccountDetail.javaClass.simpleName)
                },
                onOpenProviders = {
                    navController.navigate(PassiDestination.Providers.javaClass.simpleName)
                },
                onResumeEnrollment = {
                    navController.navigate(PassiDestination.ConfirmCode.javaClass.simpleName)
                },
                externalErrorMessage = externalErrorMessage,
                onExternalErrorShown = {
                    backStackEntry.savedStateHandle.remove<String>(AccountsErrorKey)
                },
            )
        }
        composable(PassiDestination.AccountDetail.javaClass.simpleName) {
            AccountDetailRoute(
                onManageDevices = {
                    navController.navigate(PassiDestination.AccountDevices.javaClass.simpleName)
                },
                onUpdateCertificate = {
                    navController.navigate(PassiDestination.UpdateCertificate.javaClass.simpleName)
                },
                onBack = { navController.popBackStack() }
            )
        }
        composable(PassiDestination.AccountDevices.javaClass.simpleName) {
            AccountDevicesRoute(
                onBack = { navController.popBackStack() }
            )
        }
        composable(PassiDestination.UpdateCertificate.javaClass.simpleName) {
            UpdateCertificateRoute(
                onBack = { navController.popBackStack() }
            )
        }
        composable(PassiDestination.Providers.javaClass.simpleName) {
            ProvidersRoute(
                onBack = { navController.popBackStack() },
                onOpenProvider = {
                    navController.navigate(PassiDestination.ProviderDetail.javaClass.simpleName)
                },
                onAddProvider = {
                    navController.navigate(PassiDestination.ProviderEditor.javaClass.simpleName)
                }
            )
        }
        composable(PassiDestination.ProviderDetail.javaClass.simpleName) {
            ProviderDetailRoute(
                onBack = { navController.popBackStack() },
                onEdit = {
                    navController.navigate(PassiDestination.ProviderEditor.javaClass.simpleName)
                }
            )
        }
        composable(PassiDestination.ProviderEditor.javaClass.simpleName) {
            ProviderEditorRoute(
                onBack = { navController.popBackStack() },
                onSaved = { navController.popBackStack() }
            )
        }
        composable(PassiDestination.Terms.javaClass.simpleName) {
            TermsRoute(
                onAgree = {
                    navController.navigate(PassiDestination.AddAccount.javaClass.simpleName)
                },
                onCancel = {
                    navController.popBackStack()
                }
            )
        }
        composable(PassiDestination.AddAccount.javaClass.simpleName) {
            AddAccountRoute(
                onBack = {
                    navController.popBackStack()
                },
                onSignupStarted = {
                    navController.navigate(PassiDestination.ConfirmCode.javaClass.simpleName)
                }
            )
        }
        composable(PassiDestination.ConfirmCode.javaClass.simpleName) {
            ConfirmCodeRoute(
                onCancel = {
                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                },
                onComplete = {
                    navController.navigate(PassiDestination.FinishEnrollment.javaClass.simpleName)
                }
            )
        }
        composable(PassiDestination.FinishEnrollment.javaClass.simpleName) {
            FinishEnrollmentRoute(
                onCancel = {
                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                },
                onDone = {
                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                }
            )
        }
        composable(PassiDestination.SessionChallenge.javaClass.simpleName) {
            SessionChallengeRoute(
                onCancel = { errorMessage ->
                    if (!errorMessage.isNullOrBlank()) {
                        navController
                            .getBackStackEntry(PassiDestination.Accounts.javaClass.simpleName)
                            .savedStateHandle[AccountsErrorKey] = errorMessage
                    }

                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                },
                onRequirePin = {
                    navController.navigate(PassiDestination.SessionPin.javaClass.simpleName)
                },
                onAuthorized = {
                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                }
            )
        }
        composable(PassiDestination.SessionPin.javaClass.simpleName) {
            SessionPinRoute(
                onCancel = {
                    navController.popBackStack()
                },
                onAuthorized = {
                    navController.popBackStack(
                        PassiDestination.Accounts.javaClass.simpleName,
                        inclusive = false,
                    )
                }
            )
        }
    }
}