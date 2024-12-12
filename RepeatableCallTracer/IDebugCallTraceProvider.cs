namespace RepeatableCallTracer
{
    public interface IDebugCallTraceProvider
    {
        bool IsDebug(Type targetType);

        CallTrace GetTrace(Type targetType);
    }
}
