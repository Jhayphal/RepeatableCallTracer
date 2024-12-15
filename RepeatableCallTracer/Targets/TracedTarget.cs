using System.Linq.Expressions;
using System.Reflection;

using RepeatableCallTracer.Debuggers;
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
        private readonly Type targetType = typeof(TTarget);

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

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            ITracedTargetDependenciesProvider dependenciesProvider,
            CallTracerOptions options)
            : this(
                  target,
                  dependenciesProvider,
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        public TracedTarget(
            TTarget target,
            ICallTraceWriter traceWriter,
            CallTracerOptions options)
            : this(
                  target,
                  new ReflectionBasedTracedDependenciesProvider(options),
                  traceWriter,
                  new DebugCallTraceProvider(),
                  options)
        {
        }

        protected TTarget Target { get; } = target;

        protected ITracedTargetDependenciesProvider DependenciesProvider { get; } = dependenciesProvider;

        protected ICallTraceWriter TraceWriter { get; } = traceWriter;

        protected IDebugCallTraceProvider DebugTraceProvider { get; } = debugTraceProvider;

        private bool IsDebug(MethodBase method)
            => DebugTraceProvider.IsDebug(targetType, method);

        protected ITracedTargetOperation BeginOperation(Expression<Action> expression)
        {
            if (expression.Body is not MethodCallExpression call)
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return BeginOperation(call.Method);
        }

        protected ITracedTargetOperation BeginOperation<TResult>(Expression<Func<TResult>> expression)
        {
            if (expression.Body is not MethodCallExpression call)
            {
                throw new ArgumentException("The expression must contain only a method call.", nameof(expression));
            }

            return BeginOperation(call.Method);
        }

        protected ITracedTargetOperation BeginOperation(MethodBase method)
            => IsDebug(method)
                ? BeginDebug(method)
                : BeginCall(method);

        private TracedTargetCallScope BeginCall(MethodBase method)
        {
            var methodSignature = method.ToString();
            ArgumentException.ThrowIfNullOrWhiteSpace(methodSignature);

            var expectedParameters = method
                .GetParameters()
                .ToDictionary(p => p.Name!, p => p.ParameterType);

            var assemblyVersion = targetType.Assembly.GetName().Version;
            ArgumentNullException.ThrowIfNull(assemblyVersion);

            var trace = new CallTrace
            {
                AssemblyVersion = assemblyVersion,
                AssemblyQualifiedName = targetType.AssemblyQualifiedName!,
                MethodSignature = methodSignature,
                Created = DateTime.UtcNow
            };

            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            
            return new TracedTargetCallScope(
                trace,
                options,
                expectedParameters,
                dependencies,
                TraceWriter);
        }

        private TracedTargetDebugScope BeginDebug(MethodBase method)
        {
            var trace = DebugTraceProvider.GetTrace(targetType, method);
            var dependencies = DependenciesProvider.RetrieveDependenciesAndValidateIfRequired(Target);
            
            return new TracedTargetDebugScope(trace, dependencies);
        }
    }
}
