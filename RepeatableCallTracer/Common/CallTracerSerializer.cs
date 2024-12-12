using System.Text.Json;

namespace RepeatableCallTracer.Common
{
    internal readonly struct CallTracerSerializer(CallTracerOptions options)
    {
        private readonly CallTracerOptions options = options;

        public string SerializeAndCheckDeserializationIfRequired<TValue>(TValue value, Func<TValue?, TValue?, bool> equals)
        {
            var parameterJson = JsonSerializer.Serialize(value);

            if (options.ThrowIfValueCannotBeDeserializedCorrectly)
            {
                var newValue = JsonSerializer.Deserialize<TValue>(parameterJson);

                if (!equals(value, newValue))
                {
                    throw new ArgumentException($"The type '{typeof(TValue).FullName}' cannot be deserialized correctly.");
                }
            }

            return parameterJson;
        }
    }
}
