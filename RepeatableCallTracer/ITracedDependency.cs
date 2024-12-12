namespace RepeatableCallTracer
{
    public interface ITracedDependency : ITracedDependencyOperation, ITracedDependencyDebuggable
    {
        string AssemblyQualifiedName { get; }
    }
}
