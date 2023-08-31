using Google.Cloud.Diagnostics.Common;
using GoogleTracer;
using PostSharp.Aspects;
using PostSharp.Serialization;
using System.Reflection;

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
            var type = args.Instance.GetType();
            if (!IsPropertyMethod(args.Method) && _tracer != null)
                using (_tracer.StartSpan(type.FullName + "." + args.Method.Name))
                    base.OnInvoke(args);
            else
                    base.OnInvoke(args);
        }

        private bool IsPropertyMethod(MethodBase method)
        {
            return method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"));
        }

        //public override Task OnInvokeAsync(MethodInterceptionArgs args)
        //{
        //    if (_tracer != null)
        //        using (_tracer.StartSpan(args.Method.Name))
        //            return base.OnInvokeAsync(args);
        //    else
        //    {
        //        return base.OnInvokeAsync(args);
        //    }
        //}


    }
}
