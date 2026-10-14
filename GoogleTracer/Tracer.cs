using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using ArxOne.MrAdvice.Advice;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleTracer
{
    public static class Tracer
    {
        // Backed by AsyncLocal rather than a plain static: IManagedTracer is meant to be
        // resolved per request, and a plain static is shared by every concurrent request
        // for the lifetime of the process, which corrupts span nesting/attribution under load.
        private static readonly AsyncLocal<IManagedTracer> _currentTracer = new AsyncLocal<IManagedTracer>();

        public static IManagedTracer CurrentTracer
        {
            get => _currentTracer.Value;
            set => _currentTracer.Value = value;
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
                    ProjectId = projectId,
                    Options =  Google.Cloud.Diagnostics.Common.TraceOptions.Create(0.1d, BufferOptions.TimedBuffer(TimeSpan.FromSeconds(5)))
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

            services.AddHttpClient("tracesOutgoing").AddOutgoingGoogleTraceHandler();

            services.AddSingleton<Action<HttpRequestMessage, ITraceContext>>(
                (request, traceContext) => request.Headers.Add("custom_trace_id", traceContext.TraceId));
        }

        /// <summary>
        /// Sets <see cref="CurrentTracer"/> from the current request's own DI scope on every
        /// request, instead of once at app-startup time from the root container. Must run early
        /// in the pipeline, before any [Profile]-attributed code executes.
        /// </summary>
        public static IApplicationBuilder UseCurrentTracerPerRequest(this IApplicationBuilder app)
        {
            return app.Use((context, next) =>
            {
                CurrentTracer = context.RequestServices.GetService<IManagedTracer>();
                return next();
            });
        }

        [DebuggerStepThrough]
        public static void OnInvoke(MethodAdviceContext context)
        {
            if (CurrentTracer != null && !IsPropertyMethod(context))
            {
                var targetMethodName = "";
                if (context.Target == null)
                    targetMethodName = context.TargetType.Name + " " + context.TargetName;
                else
                    targetMethodName = context.Target.GetType().Name + " " + context.TargetName;
                using (CurrentTracer.StartSpan(targetMethodName))
                    context.Proceed();
            }
            else
            {
                context.Proceed();
            }
        }

        private static bool IsPropertyMethod(MethodAdviceContext context)
        {
            return context.TargetMethod.IsSpecialName && (context.TargetMethod.Name.StartsWith("get_") || context.TargetMethod.Name.StartsWith("set_") || context.TargetMethod.Name.StartsWith(".ctor"));
        }
    }
}