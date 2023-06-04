using Android.Hardware.Biometrics;

namespace passi_android.Droid.FingerPrint;

public interface IBiometricHelper
{
    void RegisterOrAuthenticate();
    BiometricPrompt.AuthenticationCallback GetAuthenticationCallback();
}