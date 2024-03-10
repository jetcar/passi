using System;
using ArxOne.MrAdvice.Advice;

namespace GoogleTracer;

public class ProfileAttribute : Attribute, IMethodAdvice
{
    public void Advise(MethodAdviceContext context)
    {
        Tracer.OnInvoke(context);
    }
}