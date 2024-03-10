﻿using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using ArxOne.MrAdvice.Advice;
using Google.Cloud.Diagnostics.AspNetCore3;
using Google.Cloud.Diagnostics.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleTracer
{
    public class Tracer
    {
        public static IManagedTracer CurrentTracer { get; set; }

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

        public static void OnInvoke(MethodAdviceContext context)
        {
            if (CurrentTracer != null && !IsPropertyMethod(context))
            {
                var targetMethodName = "";
                if (context.Target == null)
                    targetMethodName = context.TargetType.Name + " " + context.TargetName;
                else
                    targetMethodName = context.Target.ToString() + " " + context.TargetMethod.Name;
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
            return context.TargetMethod.IsSpecialName && (context.TargetMethod.Name.StartsWith("get_") || context.TargetMethod.Name.StartsWith("set_"));
        }
    }
}