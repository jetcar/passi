using Google.Cloud.Diagnostics.Common;

namespace GoogleTracer
{
    public class Tracer
    {
        public static IManagedTracer CurrentTracer { get; set; }

    }
}