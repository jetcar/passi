using Android.Hardware.Biometrics;

namespace MauiApp2.FingerPrint;

public interface IBiometricHelper
{
    void RegisterOrAuthenticate();

    BiometricPrompt.AuthenticationCallback GetAuthenticationCallback();
}