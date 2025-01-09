using System.Diagnostics;
using System.Linq;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore
{
    public sealed class DatabaseCallTraceWriter(IDbContextFactory<CallTracerDbContext> factory)
        : ICallTraceReader, ICallTraceWriter
    {
        private readonly IDbContextFactory<CallTracerDbContext> factory = factory;

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

            if (trace is null)
            {
                return null;
            }

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

        public CallTrace GetRequiredTrace<TTarget>(DateTime time)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CallTrace> GetTraces<TTarget>()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CallTrace> GetTraces(Version assemblyVersion)
        {
            throw new NotImplementedException();
        }
    }
}
