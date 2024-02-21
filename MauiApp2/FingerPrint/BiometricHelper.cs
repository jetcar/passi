using System.Text;
using Android.Content.PM;
using Android.Hardware.Biometrics;
using Android.OS;
using Android.Runtime;
using Android.Security.Keystore;
using Android.Util;
using AppCommon;
using Java.Lang;
using Java.Security;
using Java.Security.Spec;
using MauiApp2.Platforms.Android;
using MauiApp2.utils.Services;
using MauiViewModels;
using MauiViewModels.FingerPrint;
using MauiViewModels.utils.Services;
using Signature = Java.Security.Signature;

namespace MauiApp2.FingerPrint
{
    public class BiometricHelper : IBiometricHelper
    {
        private const string KEY_STORE_NAME = "AndroidKeyStore";
        private const string KEY_NAME = "BiometricKey";
        private string REPLAY_ID = "";// TODO: Set random value?
        private const string SIGNATURE_ALGORITHM = "SHA256withECDSA";

        private BiometricPrompt biometricPrompt;
        private string signatureMessage;
        private ISecureRepository _secureRepository;
        private MainActivity _activity;

        public BiometricHelper(ISecureRepository secureRepository)
        {
            _secureRepository = secureRepository;
            _activity = MainActivity.Instance;
            REPLAY_ID = _secureRepository.GetReplyId();
        }

        public void RegisterOrAuthenticate()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.P)
            {
                CommonApp.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Fingerprint not supported below Android 9 Pie." });
                return;
            }

            // TODO: How do we determine whether to register or authenticate?
            if (string.IsNullOrEmpty(REPLAY_ID))
                Register();
            else
                Authenticate();
        }

        private void Register()
        {
            if (IsSupported)
            {
                // Generate key pair and init signature
                Signature signature;
                try
                {
                    REPLAY_ID = _secureRepository.SetReplyId();
                    // Before generating a key pair, we have to check enrollment of biometrics on the device but there is no such method on new biometric prompt API
                    // Note that this method will throw an exception if there is no enrolled biometric on the device
                    // This issue is reported to Android issue tracker: https://issuetracker.google.com/issues/112495828
                    KeyPair keyPair = GenerateKeyPair(KEY_NAME, true);
                    // Send public key part of key pair to the server, to be used for authentication
                    signatureMessage = $"{(Base64.EncodeToString(keyPair.Public.GetEncoded(), Base64Flags.UrlSafe))}:{KEY_NAME}:{REPLAY_ID}";
                    signature = InitSignature(KEY_NAME);
                }
                catch (Java.Lang.Exception e)
                {
                    throw new RuntimeException(e);
                }

                if (signature != null)
                    ShowBiometricPrompt(signature);
            }
        }

        private void Authenticate()
        {
            if (IsSupported)
            {
                // Init signature
                Signature signature;
                try
                {
                    // Send key name and challenge to the server, this message will be verified with registered public key on the server
                    signatureMessage = $"{KEY_NAME}:{REPLAY_ID}";
                    signature = InitSignature(KEY_NAME);
                }
                catch (Java.Lang.Exception e)
                {
                    throw new RuntimeException(e);
                }

                if (signature != null)
                    ShowBiometricPrompt(signature);
            }
        }

        private void ShowBiometricPrompt(Signature signature)
        {
            // Create biometric prompt
            var activity = _activity;
            biometricPrompt = new BiometricPrompt.Builder(activity)
                .SetDescription("Fingerprint")
                .SetTitle("Add Fingerprint")
                .SetSubtitle("Fingerprint")
                .SetNegativeButton("Cancel", _activity.MainExecutor, new DialogEvent())
                .Build();

            // Show biometric prompt
            var cancellationSignal = new CancellationSignal();
            var authenticationCallback = GetAuthenticationCallback();
            biometricPrompt.Authenticate(new BiometricPrompt.CryptoObject(signature), cancellationSignal, activity.MainExecutor, authenticationCallback);
        }

        public BiometricPrompt.AuthenticationCallback GetAuthenticationCallback()
        {
            // Callback for biometric authentication result
            var callback = new BiometricAuthenticationCallback
            {
                Success = (BiometricPrompt.AuthenticationResult result) =>
                {
                    var signature = result.CryptoObject.Signature;
                    try
                    {
                        signature.Update(Encoding.ASCII.GetBytes(signatureMessage));
                        var signatureString = Base64.EncodeToString(signature.Sign(), Base64Flags.UrlSafe);
                        // Normally, ToBeSignedMessage and Signature are sent to the server and then verified
                        // TODO: Toast.MakeText (getApplicationContext (), signatureMessage + ":" + signatureString, Toast.LENGTH_SHORT).show ();
                        CommonApp.FingerPrintReadingResult.Invoke(new FingerPrintResult());
                    }
                    catch (SignatureException)
                    {
                        CommonApp.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
                    }
                },
                Failed = () =>
                {
                    CommonApp.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
                },
                Help = (BiometricAcquiredStatus helpCode, ICharSequence helpString) =>
                {
                    // TODO: What do we do here?
                }
            };
            return callback;
        }

        private KeyPair GenerateKeyPair(string keyName, bool invalidatedByBiometricEnrollment)
        {
            var keyPairGenerator = KeyPairGenerator.GetInstance(KeyProperties.KeyAlgorithmEc, KEY_STORE_NAME);
            var builder = new KeyGenParameterSpec.Builder(keyName, KeyStorePurpose.Sign)
                .SetAlgorithmParameterSpec(new ECGenParameterSpec("secp256r1"))
                .SetDigests(KeyProperties.DigestSha256, KeyProperties.DigestSha384, KeyProperties.DigestSha512)
                // Require the user to authenticate with a biometric to authorize every use of the key
                .SetUserAuthenticationRequired(true)
                // Generated keys will be invalidated if the biometric templates are added more to user device
                .SetInvalidatedByBiometricEnrollment(invalidatedByBiometricEnrollment);

            keyPairGenerator.Initialize(builder.Build());

            return keyPairGenerator.GenerateKeyPair();
        }

        private KeyPair GetKeyPair(string keyName)
        {
            KeyPairGenerator kpg = KeyPairGenerator.GetInstance(
                KeyProperties.KeyAlgorithmEc, "AndroidKeyStore");
            kpg.Initialize(new KeyGenParameterSpec.Builder(
                    keyName, KeyStorePurpose.Sign | KeyStorePurpose.Verify | KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
                .SetDigests(KeyProperties.DigestSha256,
                    KeyProperties.DigestSha512).Build());

            KeyPair kp = kpg.GenerateKeyPair();

            return kp;
        }

        private Signature InitSignature(string keyName)
        {
            var keyPair = GetKeyPair(keyName);

            if (keyPair != null)
            {
                var signature = Signature.GetInstance(SIGNATURE_ALGORITHM);
                signature.InitSign(keyPair.Private);

                return signature;
            }
            return null;
        }

        /*
         * Before generating a key pair with biometric prompt, we need to check that the device supports fingerprint, iris, or face.
         * Currently, there are no FEATURE_IRIS or FEATURE_FACE constants on PackageManager.
         */

        private bool IsSupported
        {
            get
            {
                var packageManager = _activity.PackageManager;
                return packageManager.HasSystemFeature(PackageManager.FeatureFingerprint);
            }
        }

        public class BiometricAuthenticationCallback : BiometricPrompt.AuthenticationCallback
        {
            public Action<BiometricPrompt.AuthenticationResult> Success;
            public Action Failed;
            public Action<BiometricAcquiredStatus, ICharSequence> Help;

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
            {
                base.OnAuthenticationSucceeded(result);
                Success(result);
            }

            public override void OnAuthenticationFailed()
            {
                base.OnAuthenticationFailed();
                Failed();
            }

            public override void OnAuthenticationHelp([GeneratedEnum] BiometricAcquiredStatus helpCode, ICharSequence helpString)
            {
                base.OnAuthenticationHelp(helpCode, helpString);
                Help(helpCode, helpString);
            }
        }
    }
}