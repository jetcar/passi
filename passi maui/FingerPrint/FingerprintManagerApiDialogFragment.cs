using Android.Hardware.Fingerprints;
using Android.Util;
using AndroidX.Core.Hardware.Fingerprint;
using Java.Lang;
using Javax.Crypto;

namespace passi_maui.FingerPrint
{
    public class SimpleAuthCallbacks : FingerprintManagerCompat.AuthenticationCallback
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        private static readonly string TAG = "X:" + typeof(SimpleAuthCallbacks).Name;

        private static readonly byte[] SECRET_BYTES = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

        public SimpleAuthCallbacks()
        {
        }

        public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
        {
            Log.Debug(TAG, "OnAuthenticationSucceeded");
            if (result.CryptoObject.Cipher != null)
            {
                try
                {
                    // Calling DoFinal on the Cipher ensures that the encryption worked.
                    byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(SECRET_BYTES);
                    Log.Debug(TAG, "Fingerprint authentication succeeded, doFinal results: {0}",
                              Convert.ToBase64String(doFinalResult));
                }
                catch (BadPaddingException bpe)
                {
                    Log.Error(TAG, "Failed to encrypt the data with the generated key." + bpe);
                }
                catch (IllegalBlockSizeException ibse)
                {
                    Log.Error(TAG, "Failed to encrypt the data with the generated key." + ibse);
                }
            }
            else
            {
                // No cipher used, assume that everything went well and trust the results.
                Log.Debug(TAG, "Fingerprint authentication succeeded.");
            }
        }

        public override void OnAuthenticationError(int errMsgId, ICharSequence errString)
        {
            // There are some situations where we don't care about the error. For example,
            // if the user cancelled the scan, this will raise errorID #5. We don't want to
            // report that, we'll just ignore it as that event is a part of the workflow.
            bool reportError = (errMsgId == (int)FingerprintState.ErrorCanceled);

            string debugMsg = string.Format("OnAuthenticationError: {0}:`{1}`.", errMsgId, errString);

            {
                debugMsg += " Ignoring the error.";
            }
            Log.Debug(TAG, debugMsg);
        }

        public override void OnAuthenticationFailed()
        {
            Log.Info(TAG, "Authentication failed.");
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            Log.Debug(TAG, "OnAuthenticationHelp: {0}:`{1}`", helpString, helpMsgId);
        }
    }
}