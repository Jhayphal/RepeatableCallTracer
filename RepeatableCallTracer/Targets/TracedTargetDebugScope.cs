namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetDebugScope(
        CallTrace trace,
        IEnumerable<ITracedDependencyDebuggable> dependencies) : ITracedTargetOperation
    {
        private readonly CallTrace trace = trace;
        private readonly IEnumerable<ITracedDependencyDebuggable> dependencies = dependencies;

        public void BeginDebug()
        {
            foreach (var dependency in dependencies)
            {
                dependency.AttachDebugger(trace);
            }
        }

        public void Dispose()
        {
            foreach (var dependency in dependencies.Reverse())
            {
                dependency.DetachDebugger();
            }
        }

        public TParameter SetParameter<TParameter>(string name, TParameter value) where TParameter : IEquatable<TParameter>
            => SetParameter(name, value, EqualityComparer<TParameter>.Default.Equals);

        public TParameter SetParameter<TParameter>(string name, TParameter value, EqualityComparer<TParameter> equalityComparer)
            => SetParameter(name, value, EqualityComparer<TParameter>.Default.Equals);

        public TParameter SetParameter<TParameter>(string name, TParameter value, Func<TParameter, TParameter, bool> equals)
            => trace.GetTargetMethodParameter<TParameter>(name);
    }
}
