using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepeatableCallTracer
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public sealed class CallTrace
    {
        public CallTrace(Version assemblyVersion, string assemblyQualifiedName, string methodSignature, DateTime created)
        {
            AssemblyVersion = assemblyVersion;
            AssemblyQualifiedName = assemblyQualifiedName;
            MethodSignature = methodSignature;
            Created = created;
        }

        public Version AssemblyVersion { get; }

        public string AssemblyQualifiedName { get; }

        public string MethodSignature { get; }

        public DateTime Created { get; }

        public TimeSpan Elapsed { get; set; }

        public string Error { get; internal set; }

        [JsonIgnore]
        public bool Failed
            => string.IsNullOrWhiteSpace(Error);

        public Dictionary<string, string> MethodParameters { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Dependency Key, Method, Call Id, Method Result.
        /// </summary>
        public Dictionary<string, Dictionary<string, Dictionary<int, string>>> ProvidedData { get; } = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();

        [JsonIgnore]
        internal string Key
            => AssemblyQualifiedName + MethodSignature + Created.ToString("O");

        public TParameter GetTargetMethodParameter<TParameter>(string parameterName)
            => JsonSerializer.Deserialize<TParameter>(MethodParameters[parameterName]);

        public TResult GetDependencyMethodResult<TResult>(
            ITracedDependency dependency,
            MethodBase method,
            int callId)
        {
            var methodSignature = method.ToString();
            if (string.IsNullOrWhiteSpace(methodSignature))
            {
                throw new ArgumentException("Method signature cannot be empty.", nameof(methodSignature));
            }

            var content = ProvidedData[dependency.AssemblyQualifiedName][methodSignature][callId];

            return JsonSerializer.Deserialize<TResult>(content);
        }

        internal void SetDependencyMethodResult(
            ITracedDependency dependency,
            MethodBase method,
            int callId,
            string methodResultJson)
        {
            var methodSignature = method.ToString();
            if (string.IsNullOrWhiteSpace(methodSignature))
            {
                throw new ArgumentException("Method signature cannot be empty.", nameof(methodSignature));
            }

            if (!ProvidedData.TryGetValue(dependency.AssemblyQualifiedName, out var methods))
            {
                methods = new Dictionary<string, Dictionary<int, string>>();

                ProvidedData.Add(dependency.AssemblyQualifiedName, methods);
            }

            if (!methods.TryGetValue(methodSignature, out var calls))
            {
                calls = new Dictionary<int, string>();

                methods.Add(methodSignature, calls);
            }

            calls.Add(callId, methodResultJson);
        }
    }
}
