namespace RepeatableCallTracer
{
    public interface ITracedDependencyOperation
    {
        void BeginOperation(CallTrace trace);

        void EndOperation();
    }
}
