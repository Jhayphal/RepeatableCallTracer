using System.Reflection;

using RepeatableCallTracer.Common;
using RepeatableCallTracer.Dependencies;

namespace RepeatableCallTracer.Targets
{
    public abstract class TracedTarget<TTarget>(
        TTarget target,
        ITracedTargetDependenciesProvider dependenciesProvider,
        ICallTraceWriter traceWriter,
        IDebugCallTraceProvider debugTraceProvider,
        CallTracerOptions options)
    {
        private readonly CallTracerSerializer serializer = new(options);
        private readonly CallTracerValidator validator = new(options);

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            IDebugCallTraceProvider debugTraceProvider,
            CallTracerOptions options)
            : this(
                  target,
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  debugTraceProvider,
                  options)
        {
        }

        protected TTarget Target { get; } = target;

        protected ITracedTargetDependenciesProvider DependenciesProvider { get; } = dependenciesProvider;

        protected ICallTraceWriter TraceWriter { get; } = traceWriter;

        protected IDebugCallTraceProvider DebugTraceProvider { get; } = debugTraceProvider;

        private bool IsDebug
            => DebugTraceProvider.IsDebug(typeof(TTarget));

        protected ITracedTargetOperation BeginOperation(MethodBase method)
            => IsDebug
                ? BeginDebug()
                : BeginScope(method);

        private TracedTargetCallScope BeginScope(MethodBase method)
        {
            ArgumentNullException.ThrowIfNull(method);

            var methodSignature = method.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);

            var targetType = typeof(TTarget);
            var assemblyVersion = targetType.Assembly.GetName().Version;
            ArgumentNullException.ThrowIfNull(assemblyVersion);

            var snapshot = new CallTrace
            {
                AssemblyVersion = assemblyVersion,
                AssemblyQualifiedName = targetType.AssemblyQualifiedName!,
                MethodSignature = methodSignature,
                Created = DateTime.UtcNow
            };

            var expectedParameters = method.GetParameters();
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            var scope = new TracedTargetCallScope(
                serializer,
                validator,
                TraceWriter,
                snapshot,
                dependencies,
                expectedParameters.ToDictionary(p => p.Name!, p => p.ParameterType));

            scope.BeginOperation();

            return scope;
        }

        private TracedTargetDebugScope BeginDebug()
        {
            var trace = DebugTraceProvider.GetTrace(typeof(TTarget));
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            var scope = new TracedTargetDebugScope(trace, dependencies);
            scope.BeginDebug();

            return scope;
        }
    }
}
