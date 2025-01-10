using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class CallTracesStorage(IDbContextFactory<CallTracerDbContext> factory)
        : ICallTraceReader, ICallTraceWriter
    {
        private readonly IDbContextFactory<CallTracerDbContext> factory = factory;

        public CallTrace? GetNearestTraceUpTo<TTarget>(DateTime time)
        {
            using var context = factory.CreateDbContext();

            var targetType = typeof(TTarget);
            var version = targetType.Assembly.GetName().Version!.ToString();
            var name = targetType.AssemblyQualifiedName;
            
            var trace = context.Traces
                .OrderByDescending(t => t.Created)
                .Where(t => t.AssemblyVersion == version
                    && t.AssemblyQualifiedName == name
                    && t.Created <= time)
                .FirstOrDefault();

            return trace is null
                ? null
                : ReadTrace(context, trace);
        }

        public CallTrace GetRequiredTrace<TTarget>(DateTime time)
        {
            using var context = factory.CreateDbContext();

            var targetType = typeof(TTarget);
            var version = targetType.Assembly.GetName().Version!.ToString();
            var name = targetType.AssemblyQualifiedName;

            var trace = context.Traces
                .OrderByDescending(t => t.Created)
                .Where(t => t.AssemblyVersion == version
                    && t.AssemblyQualifiedName == name
                    && t.Created == time)
                .FirstOrDefault();

            return trace is null
                ? throw new ArgumentOutOfRangeException(nameof(time))
                : ReadTrace(context, trace);
        }

        public IEnumerable<CallTrace> GetTraces<TTarget>()
        {
            using var context = factory.CreateDbContext();

            var targetType = typeof(TTarget);
            var version = targetType.Assembly.GetName().Version!.ToString();
            var name = targetType.AssemblyQualifiedName;

            var traces = context.Traces
                .OrderByDescending(t => t.Created)
                .Where(t => t.AssemblyVersion == version
                    && t.AssemblyQualifiedName == name)
                .ToList();

            List<CallTrace> result = [];

            foreach (var item in traces)
            {
                var trace = ReadTrace(context, item)
                    ?? throw new InvalidDataException();

                result.Add(trace);
            }

            return result;
        }

        public IEnumerable<CallTrace> GetTraces(Version assemblyVersion)
        {
            using var context = factory.CreateDbContext();

            var version = assemblyVersion.ToString();

            var traces = context.Traces
                .OrderByDescending(t => t.Created)
                .Where(t => t.AssemblyVersion == version)
                .ToList();

            List<CallTrace> result = [];

            foreach (var item in traces)
            {
                var trace = ReadTrace(context, item)
                    ?? throw new InvalidDataException();

                result.Add(trace);
            }

            return result;
        }

        public void Append(CallTrace trace)
        {
            using var context = factory.CreateDbContext();

            var targetMethodCall = new TargetMethodCall
            {
                AssemblyVersion = trace.AssemblyVersion.ToString(),
                AssemblyQualifiedName = trace.AssemblyQualifiedName,
                MethodSignature = trace.MethodSignature,
                Created = trace.Created,
                Elapsed = trace.Elapsed,
                Error = trace.Error
            };

            context.Traces.Add(targetMethodCall);

            foreach (var methodParameter in trace.MethodParameters)
            {
                context.Parameters.Add(new TargetMethodParameter
                {
                    TargetMethodCallId = targetMethodCall.Id,
                    Name = methodParameter.Key,
                    Value = methodParameter.Value
                });
            }

            foreach (var dependency in trace.ProvidedData)
            {
                foreach (var method in dependency.Value)
                {
                    var dependencyMethod = new DependencyMethod
                    {
                        TargetMethodCallId = targetMethodCall.Id,
                        DependencyKey = dependency.Key,
                        MethodSignature = method.Key,
                    };

                    context.Dependencies.Add(dependencyMethod);

                    foreach (var call in method.Value)
                    {
                        context.DependencyMethodCalls.Add(new DependencyMethodCall
                        {
                            DependencyMethodId = dependencyMethod.Id,
                            CallId = call.Key,
                            MethodResult = call.Value
                        });
                    }
                }
            }

            context.SaveChanges();
        }

        private static CallTrace ReadTrace(
            CallTracerDbContext context,
            TargetMethodCall trace)
        {
            var result = new CallTrace(
                assemblyVersion: Version.Parse(trace.AssemblyVersion),
                assemblyQualifiedName: trace.AssemblyQualifiedName,
                methodSignature: trace.MethodSignature,
                created: trace.Created)
            {
                Elapsed = trace.Elapsed
            };

            var methodParameters = context.Parameters
                .Where(p => p.TargetMethodCallId == trace.Id)
                .ToList();

            foreach (var @param in methodParameters)
            {
                result.MethodParameters.Add(@param.Name, @param.Value);
            }

            var dependencies = context.Dependencies
                .Where(d => d.TargetMethodCallId == trace.Id)
                .ToList();

            foreach (var dependency in dependencies)
            {
                var calls = context.DependencyMethodCalls
                    .Where(c => c.DependencyMethodId == dependency.Id)
                    .ToDictionary(c => c.CallId, c => c.MethodResult);

                if (!result.ProvidedData.TryGetValue(dependency.DependencyKey, out var methods))
                {
                    methods = [];

                    result.ProvidedData.Add(dependency.DependencyKey, methods);
                }

                methods.Add(dependency.MethodSignature, calls);
            }

            return result;
        }
    }
}
