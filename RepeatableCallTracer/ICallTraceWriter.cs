namespace RepeatableCallTracer
{
    public interface ICallTraceWriter
    {
        void Append(CallTrace trace);
    }
}
