namespace RepeatableCallTracer
{
    public interface ITracedTargetDependenciesProvider
    {
        IEnumerable<ITracedDependency> RetrieveDependenciesAndValidateIfRequired<TTarget>(TTarget target);
    }
}
