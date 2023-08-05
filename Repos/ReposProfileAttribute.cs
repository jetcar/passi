using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace Repos
{
    [PSerializable]
    public class ReposProfileAttribute : MethodInterceptionAspect
    {
        public static IManagedTracer _tracer
        {
            get { return Tracer.CurrentTracer; }
        }


        public override void OnInvoke(MethodInterceptionArgs args)
        {
            if (_tracer != null)
                using (_tracer.StartSpan(args.Method.Name))
                    base.OnInvoke(args);
            else
            {
                base.OnInvoke(args);
            }
        }

    }
}
