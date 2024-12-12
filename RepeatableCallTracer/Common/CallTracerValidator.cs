﻿namespace RepeatableCallTracer.Common
{
    internal readonly struct CallTracerValidator(CallTracerOptions options)
    {
        private readonly CallTracerOptions options = options;

        public void CheckMethodParametersIfRequired(
            IReadOnlyDictionary<string, Type> expectedParameters,
            IReadOnlyDictionary<string, Type> actualParameters)
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
