using RepeatableCallTracer.Common;

namespace RepeatableCallTracer.Targets
{
    internal sealed class TracedTargetCallScope : ITracedTargetOperation
    {
        private readonly CallTrace trace;
        private readonly CallTracerOptions options;
        private readonly IReadOnlyDictionary<string, Type> expectedParameters;
        private readonly IEnumerable<ITracedDependency> dependencies;
        private readonly ICallTraceWriter traceWriter;

        private readonly CallTracerSerializer serializer;

        private readonly Dictionary<string, Type> actualParameters = [];

        public TracedTargetCallScope(
            CallTrace trace,
            CallTracerOptions options,
            IReadOnlyDictionary<string, Type> expectedParameters,
            IEnumerable<ITracedDependency> dependencies,
            ICallTraceWriter traceWriter)
        {
            this.trace = trace;
            this.options = options;
            this.expectedParameters = expectedParameters;
            this.dependencies = dependencies;
            this.traceWriter = traceWriter;
            serializer = new(options);

            foreach (var operation in dependencies)
            {
                operation.BeginOperation(trace);
            }
        }

        public void Dispose()
        {
            foreach (var operation in dependencies.Reverse())
            {
                operation.EndOperation();
            }

            CheckMethodParametersIfRequired();

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

        private void CheckMethodParametersIfRequired()
        {
            if (!options.ThrowIfParametersDifferMethodSignature)
            {
                return;
            }

            HashSet<string> processedParameters = [];

            foreach (var parameter in expectedParameters)
            {
                var parameterName = parameter.Key;
                if (parameterName is not null)
                {
                    if (!actualParameters.TryGetValue(parameterName, out var parameterType))
                    {
                        throw new InvalidProgramException($"Parameter '{parameterName}' was not defined.");
                    }
                    else if (parameterType != parameter.Value)
                    {
                        throw new InvalidProgramException($"Parameter '{parameterName}' has wrong type - '{parameterType}'. Expected type is '{parameter.Value}'.");
                    }

                    processedParameters.Add(parameterName);
                }
            }

            foreach (var parameterName in actualParameters.Keys)
            {
                if (!processedParameters.Contains(parameterName))
                {
                    throw new InvalidOperationException($"Unexpected parameter '{parameterName}'.");
                }
            }
        }
    }
}
