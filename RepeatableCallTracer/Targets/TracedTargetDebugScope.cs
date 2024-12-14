namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetDebugScope : ITracedTargetOperation
    {
        private readonly CallTrace trace;
        private readonly IEnumerable<ITracedDependency> dependencies;

        public TracedTargetDebugScope(
            CallTrace trace,
            IEnumerable<ITracedDependency> dependencies)
        {
            this.trace = trace;
            this.dependencies = dependencies;

            foreach (var operation in dependencies)
            {
                operation.AttachDebugger(trace);
            }
        }

        public void Dispose()
        {
            foreach (var operation in dependencies.Reverse())
            {
                operation.DetachDebugger();
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
