namespace RepeatableCallTracer
{
    public interface ITracedOperation
    {
        void Begin(CallTrace trace);

        void End();
    }
}
