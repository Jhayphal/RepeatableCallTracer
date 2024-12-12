namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetDebugScope : ITracedTargetOperation
    {
        private readonly CallTrace trace;
        private readonly IEnumerable<ITracedOperation> operations;

        public TracedTargetDebugScope(
            CallTrace trace,
            IEnumerable<ITracedOperation> operations)
        {
            this.trace = trace;
            this.operations = operations;

            foreach (var operation in operations)
            {
                operation.Begin(trace);
            }
        }

        public void Dispose()
        {
            foreach (var operation in operations.Reverse())
            {
                operation.End();
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
