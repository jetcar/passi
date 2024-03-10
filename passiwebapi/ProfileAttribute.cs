﻿using System;
using ArxOne.MrAdvice.Advice;
using GoogleTracer;

public class PassiProfileAttribute : Attribute, IMethodAdvice
{
    public void Advise(MethodAdviceContext context)
    {
        Tracer.OnInvoke(context);
    }
}