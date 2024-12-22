using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        private readonly Stopwatch stopwatch;

        private readonly CallTracerSerializer serializer;

        private readonly Dictionary<string, Type> actualParameters = new Dictionary<string, Type>();

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
            serializer = new CallTracerSerializer(options);

            foreach (var operation in dependencies)
            {
                operation.BeginOperation(trace);
            }

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void Dispose()
        {
            stopwatch.Stop();

            trace.Elapsed = stopwatch.Elapsed;

            foreach (var operation in dependencies.Reverse())
            {
                operation.EndOperation();
            }

            CheckMethodParametersIfRequired();

            traceWriter.Append(trace);
        }

        public void SetError(string error)
            => trace.Error = error;

        public void SetError(Exception error)
            => SetError(error.ToString());

        public void SetParameter<TParameter>(string name, ref TParameter value) where TParameter : IEquatable<TParameter>
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                EqualityComparer<TParameter>.Default.Equals);

            actualParameters[name] = typeof(TParameter);
        }

        public void SetParameter<TParameter>(
            string name,
            ref TParameter value,
            EqualityComparer<TParameter> equalityComparer)
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                equalityComparer.Equals);

            actualParameters[name] = typeof(TParameter);
        }

        public void SetParameter<TParameter>(
            string name,
            ref TParameter value,
            Func<TParameter, TParameter, bool> equals)
        {
            trace.MethodParameters[name] = serializer.SerializeAndCheckDeserializationIfRequired(
                value,
                equals);

            actualParameters[name] = typeof(TParameter);
        }

        private void CheckMethodParametersIfRequired()
        {
            if (!options.ThrowIfParametersDifferMethodSignature)
            {
                return;
            }

            var processedParameters = new HashSet<string>();

            foreach (var parameter in expectedParameters)
            {
                var parameterName = parameter.Key;
                if (!(parameterName is null))
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
