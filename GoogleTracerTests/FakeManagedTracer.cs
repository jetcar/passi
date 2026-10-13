using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Google.Cloud.Diagnostics.Common;

namespace GoogleTracerTests
{
    /// <summary>
    /// Minimal IManagedTracer stub. Only identity (reference equality) is used by
    /// the tests in this project; the span-related members are never exercised.
    /// </summary>
    internal class FakeManagedTracer : IManagedTracer
    {
        public ISpan StartSpan(string name, StartSpanOptions options = null) => throw new NotImplementedException();
        public void RunInSpan(Action action, string name, StartSpanOptions options = null) => throw new NotImplementedException();
        public T RunInSpan<T>(Func<T> func, string name, StartSpanOptions options = null) => throw new NotImplementedException();
        public Task<T> RunInSpanAsync<T>(Func<Task<T>> func, string name, StartSpanOptions options = null) => throw new NotImplementedException();
        public void AnnotateSpan(Dictionary<string, string> labels) => throw new NotImplementedException();
        public void SetStackTrace(StackTrace stackTrace) => throw new NotImplementedException();
        public string GetCurrentTraceId() => throw new NotImplementedException();
        public ulong? GetCurrentSpanId() => throw new NotImplementedException();
    }
}
