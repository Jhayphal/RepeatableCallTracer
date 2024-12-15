namespace RepeatableCallTracer
{
    public interface ITracedDependency
    {
        string AssemblyQualifiedName { get; }

        void BeginOperation(CallTrace trace);

        void EndOperation();

        void AttachDebugger(CallTrace trace);

        void DetachDebugger();
    }
}
