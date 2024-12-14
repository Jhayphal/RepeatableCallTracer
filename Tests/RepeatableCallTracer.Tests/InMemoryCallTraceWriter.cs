namespace RepeatableCallTracer.Tests;

internal sealed class InMemoryCallTraceWriter : ICallTraceWriter
{
    public LinkedList<CallTrace> Traces { get; } = [];

    public void Append(CallTrace trace)
    {
        Traces.AddLast(trace);
    }
}
