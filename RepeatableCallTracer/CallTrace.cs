using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepeatableCallTracer
{
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public sealed class CallTrace : IEquatable<CallTrace>
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

        public string Error { get; set; } = string.Empty;

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

        public bool Equals(CallTrace other)
        {
            if (other is null)
            {
                return false;
            }

            if (!(other.AssemblyVersion == AssemblyVersion
                && other.AssemblyQualifiedName == AssemblyQualifiedName
                && other.MethodSignature == MethodSignature
                && other.Created == Created
                && other.Elapsed == Elapsed
                && other.Error == Error))
            {
                return false;
            }

            if (!AreMethodParametersEquals(other.MethodParameters))
            {
                return false;
            }

            return AreProvidedDataEquals(other.ProvidedData);
        }

        public override bool Equals(object obj)
            => Equals(obj as CallTrace);

        private bool AreMethodParametersEquals(Dictionary<string, string> methodParameters)
            => AreEquals(methodParameters, MethodParameters, (a, b) => string.Equals(a, b));

        private bool AreProvidedDataEquals(Dictionary<string, Dictionary<string, Dictionary<int, string>>> providedData)
            => AreEquals(
                providedData,
                ProvidedData,
                (a, b) => AreEquals(
                    a,
                    b,
                    (x, y) => AreEquals(
                        x,
                        y,
                        (i, j) => string.Equals(i, j))));

        private static bool AreEquals<TKey, TValue>(
            IDictionary<TKey, TValue> first,
            IDictionary<TKey, TValue> second,
            Func<TValue, TValue, bool> areValuesEquals)
        {
            if (first is null)
            {
                return second is null;
            }

            if (second is null)
            {
                return false;
            }

            if (first.Count != second.Count)
            {
                return false;
            }

            foreach (var key in first.Keys)
            {
                if (!second.TryGetValue(key, out var secondValue))
                {
                    return false;
                }

                var firstValue = first[key];

                if (!areValuesEquals(firstValue, secondValue))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 129417796;
            hashCode = hashCode * -1521134295 + AssemblyVersion.GetHashCode();
            hashCode = hashCode * -1521134295 + AssemblyQualifiedName.GetHashCode();
            hashCode = hashCode * -1521134295 + MethodSignature.GetHashCode();
            hashCode = hashCode * -1521134295 + Created.GetHashCode();
            hashCode = hashCode * -1521134295 + Elapsed.GetHashCode();
            hashCode = hashCode * -1521134295 + Error.GetHashCode();
            hashCode = hashCode * -1521134295 + MethodParameters.GetHashCode();
            hashCode = hashCode * -1521134295 + ProvidedData.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(CallTrace left, CallTrace right)
            => EqualityComparer<CallTrace>.Default.Equals(left, right);

        public static bool operator !=(CallTrace left, CallTrace right)
            => !(left == right);
    }
}
