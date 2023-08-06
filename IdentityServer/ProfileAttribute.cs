using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using PostSharp.Aspects;
using PostSharp.Serialization;
using System.Threading.Tasks;

namespace IdentityServer
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
        public override Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            using (_tracer.StartSpan(args.Method.Name))
                return base.OnInvokeAsync(args);
        }

    }
}
