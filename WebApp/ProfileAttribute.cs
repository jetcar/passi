using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using PostSharp.Aspects;
using PostSharp.Serialization;
using System.Reflection;
using System.Threading.Tasks;

[PSerializable]
internal class ProfileAttribute : MethodInterceptionAspect
{
    public override void OnInvoke(MethodInterceptionArgs args)
    {
        Tracer.OnInvoke(args, base.OnInvoke);
    }

    public override Task OnInvokeAsync(MethodInterceptionArgs args)
    {
        return Tracer.OnInvokeAsync(args, base.OnInvokeAsync);
    }
}