using System;
using System.Diagnostics;
using ArxOne.MrAdvice.Advice;

namespace GoogleTracer;

public class ProfileAttribute : Attribute, IMethodAdvice
{
    [DebuggerStepThrough]
    public void Advise(MethodAdviceContext context)
    {
        Tracer.OnInvoke(context);
    }
}