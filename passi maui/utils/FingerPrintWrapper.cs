namespace passi_maui.utils
{
    public class FingerPrintWrapper
    {
        public Func<bool> HasEnrolledFingerprints { get; set; }
        public Func<bool> IsHardwareDetected { get; set; }
    }
}