using System.Reflection;

namespace RepeatableCallTracer.Common
{
    public sealed class CallTracesFactory : ICallTracesFactory
    {
        public CallTrace Create(Type targetType, MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method);

            var methodSignature = method.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);

            var assemblyVersion = targetType.Assembly.GetName().Version;
            ArgumentNullException.ThrowIfNull(assemblyVersion);

            return new CallTrace
            {
                AssemblyVersion = assemblyVersion,
                AssemblyQualifiedName = targetType.AssemblyQualifiedName!,
                MethodSignature = methodSignature,
                Created = DateTime.UtcNow
            };
        }
    }
}
