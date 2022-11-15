using Android.Support.V4.Hardware.Fingerprint;
using Android.Util;
using AppCommon;
using Java.Lang;
using Javax.Crypto;

namespace passi_android.Droid.FingerPrint
{
    public class MyAuthCallbackSample : FingerprintManagerCompat.AuthenticationCallback
    {
        private readonly MainActivity _mainActivity;
        private static readonly byte[] SECRET_BYTES = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        private static readonly string TAG = "X:" + typeof(MyAuthCallbackSample).Name;

        public MyAuthCallbackSample(MainActivity mainActivity)
        {
            _mainActivity = mainActivity;
        }

        public override void OnAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
        {
            if (result.CryptoObject.Cipher != null)
            {
                try
                {
                    // Calling DoFinal on the Cipher ensures that the encryption worked.
                    byte[] doFinalResult = result.CryptoObject.Cipher.DoFinal(SECRET_BYTES);
                    App.FingerPrintReadingResult.Invoke(new FingerPrintResult());
                }
                catch (BadPaddingException bpe)
                {
                    App.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
                }
                catch (IllegalBlockSizeException ibse)
                {
                    App.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
                }
            }
            else
            {
                App.FingerPrintReadingResult.Invoke(new FingerPrintResult());

                // No cipher used, assume that everything went well and trust the results.
            }
        }

        public override void OnAuthenticationFailed()
        {
            Log.Info(TAG, "Authentication failed.");
            App.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
        }

        public override void OnAuthenticationHelp(int helpMsgId, ICharSequence helpString)
        {
            Log.Debug(TAG, "OnAuthenticationHelp: {0}:`{1}`", helpString, helpMsgId);
            App.FingerPrintReadingResult.Invoke(new FingerPrintResult() { ErrorMessage = "Error" });
        }
    }
}