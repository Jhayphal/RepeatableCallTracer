namespace RepeatableCallTracer
{
    public interface ITracedDependencyDebuggable
    {
        void AttachDebugger(CallTrace trace);

        void DetachDebugger();
    }
}
