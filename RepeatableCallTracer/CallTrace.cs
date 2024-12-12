using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace RepeatableCallTracer
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public sealed record class CallTrace
    {
        public required Version AssemblyVersion { get; init; }

        public required string AssemblyQualifiedName { get; init; }

        public required string MethodSignature { get; init; }

        public required DateTime Created { get; init; }

        public Dictionary<string, string> MethodParameters { get; } = [];

        /// <summary>
        /// Dependency Key, Method, Call Id, Method Result.
        /// </summary>
        public Dictionary<string, Dictionary<string, Dictionary<int, string>>> ProvidedData { get; } = [];

        [JsonIgnore]
        internal string Key
            => AssemblyQualifiedName + MethodSignature + Created.ToString("O");

        public TParameter GetTargetMethodParameter<TParameter>(string parameterName)
            => JsonSerializer.Deserialize<TParameter>(MethodParameters[parameterName])!;

        public TResult GetDependencyMethodResult<TResult>(
            ITracedDependency dependency,
            MethodBase method,
            int callId)
        {
            var methodSignature = method.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);

            var content = ProvidedData[dependency.AssemblyQualifiedName][methodSignature][callId];

            return JsonSerializer.Deserialize<TResult>(content)!;
        }

        internal void SetDependencyMethodResult(
            ITracedDependency dependency,
            MethodBase method,
            int callId,
            string methodResultJson)
        {
            var methodSignature = method.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);

            if (!ProvidedData.TryGetValue(dependency.AssemblyQualifiedName, out var methods))
            {
                methods = [];

                ProvidedData.Add(dependency.AssemblyQualifiedName, methods);
            }

            if (!methods.TryGetValue(methodSignature, out var calls))
            {
                calls = [];

                methods.Add(methodSignature, calls);
            }

            calls.Add(callId, methodResultJson);
        }
    }
}
