using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace WebApp
{
    [PSerializable]
    public class ProfileAttribute : MethodInterceptionAspect
    {
        public static IManagedTracer _tracer
        {
            get { return Tracer.CurrentTracer; }
        }


        public override void OnInvoke(MethodInterceptionArgs args)
        {
            using (_tracer.StartSpan(args.Method.Name))
                base.OnInvoke(args);
        }

    }
}
