using System;

namespace passi_android
{
    public class FingerPrintWrapper
    {
        public Func<bool> HasEnrolledFingerprints { get; set; }
        public Func<bool> IsHardwareDetected { get; set; }
    }
}