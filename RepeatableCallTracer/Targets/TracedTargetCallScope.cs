using RepeatableCallTracer.Common;

namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetCallScope(
        CallTracerSerializer serializer,
        CallTracerValidator rulesChecker,
        ICallTraceWriter traceWriter,
        CallTrace trace,
        IEnumerable<ITracedDependencyOperation> operations,
        IReadOnlyDictionary<string, Type> expectedParameters) : ITracedTargetOperation
    {
        private readonly CallTracerValidator rulesChecker = rulesChecker;
        private readonly ICallTraceWriter traceWriter = traceWriter;
        private readonly CallTrace trace = trace;
        private readonly IEnumerable<ITracedDependencyOperation> operations = operations;
        private readonly IReadOnlyDictionary<string, Type> expectedParameters = expectedParameters;

        private readonly Dictionary<string, Type> actualParameters = [];

        public void BeginOperation()
        {
            foreach (var operation in operations)
            {
                operation.BeginOperation(trace);
            }
        }

        public void Dispose()
        {
            foreach (var operation in operations.Reverse())
            {
                operation.EndOperation();
            }

            rulesChecker.CheckMethodParametersIfRequired(expectedParameters, actualParameters);

            traceWriter.Append(trace);
        }

        public TParameter SetParameter<TParameter>(string name, TParameter value) where TParameter : IEquatable<TParameter>
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                EqualityComparer<TParameter>.Default.Equals);

            actualParameters.TryAdd(name, typeof(TParameter));

            return value;
        }

        public TParameter SetParameter<TParameter>(
            string name,
            TParameter value,
            EqualityComparer<TParameter> equalityComparer)
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                equalityComparer.Equals);

            actualParameters.TryAdd(name, typeof(TParameter));

            return value;
        }

        public TParameter SetParameter<TParameter>(
            string name,
            TParameter value,
            Func<TParameter?, TParameter?, bool> equals)
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                equals);

            actualParameters.TryAdd(name, typeof(TParameter));

            return value;
        }
    }
}
