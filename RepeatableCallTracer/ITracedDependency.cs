namespace RepeatableCallTracer
{
    public interface ITracedDependency : ITracedOperation
    {
        string AssemblyQualifiedName { get; }
    }
}
