using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PostSharp.Aspects;

namespace GoogleTracer
{
    public class Tracer
    {
        public static IManagedTracer CurrentTracer { get; set; }

        public static void OnInvoke(MethodInterceptionArgs args, Action<MethodInterceptionArgs> onInvoke)
        {
            var type = args.Instance.GetType();
            if (!IsPropertyMethod(args.Method) && CurrentTracer != null)
                using (CurrentTracer.StartSpan(type.FullName + "." + args.Method.Name))
                    onInvoke(args);
            else
                onInvoke(args);
        }

        public static Task OnInvokeAsync(MethodInterceptionArgs args, Func<MethodInterceptionArgs, Task> onInvokeAsync)
        {
            var type = args.Instance.GetType();
            if (!IsPropertyMethod(args.Method) && CurrentTracer != null)
                using (CurrentTracer.StartSpan(type.FullName + "." + args.Method.Name))
                    return onInvokeAsync(args);
            return onInvokeAsync(args);
        }

        private static bool IsPropertyMethod(MethodBase method)
        {
            return method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"));
        }

        public static void SetupTracer(IServiceCollection services, string projectId, string name)
        {
            services.AddScoped(CustomTraceContextProvider);
            static ITraceContext CustomTraceContextProvider(IServiceProvider sp)
            {
                var accessor = sp.GetRequiredService<IHttpContextAccessor>();
                var traceId = accessor.HttpContext?.Request?.Headers["custom_trace_id"];
                return new SimpleTraceContext(traceId, null, null);
            }

            services.AddGoogleTraceForAspNetCore(new AspNetCoreTraceOptions
            {
                ServiceOptions = new TraceServiceOptions()
                {
                    ProjectId = projectId
                },
                TraceFallbackPredicate = TraceDecisionPredicate.Create(request =>
                {
                    if (request.Path.Value.Contains(".html"))
                        return false;
                    if (request.Path.Value == "/")
                        return false;
                    if (request.Path.Value.Contains(".js"))
                        return false;
                    if (request.Path.Value.Contains(".ico"))
                        return false;
                    if (request.Path.Value.Contains(".txt"))
                        return false;
                    if (request.Path.Value.Contains(".css"))
                        return false;
                    if (request.Path.Value.Contains("health"))
                        return false;
                    return true;
                }, true)
            });
            services.AddGoogleDiagnostics(projectId, name);
            services.AddHttpClient("tracesOutgoing").AddOutgoingGoogleTraceHandler();
            services.AddSingleton<Action<HttpResponse, ITraceContext>>(
                (response, traceContext) => response.Headers.Add("custom_trace_id", traceContext.TraceId));

            services.AddSingleton<Action<HttpRequestMessage, ITraceContext>>(
                (request, traceContext) => request.Headers.Add("custom_trace_id", traceContext.TraceId));
        }
    }
}