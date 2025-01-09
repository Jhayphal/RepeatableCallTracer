namespace RepeatableCallTracer.EntityFrameworkCore
{
    public interface ICallTraceReader
    {
        CallTrace GetRequiredTrace<TTarget>(DateTime time);

        CallTrace? GetNearestTraceUpTo<TTarget>(DateTime time);

        IEnumerable<CallTrace> GetTraces<TTarget>();

        IEnumerable<CallTrace> GetTraces(Version assemblyVersion);
    }
}
