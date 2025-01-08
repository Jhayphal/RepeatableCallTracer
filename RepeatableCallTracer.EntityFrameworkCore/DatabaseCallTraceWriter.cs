using System.Diagnostics;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

namespace RepeatableCallTracer.EntityFrameworkCore
{
    public interface ICallTraceReader
    {
        CallTrace GetRequiredTrace<TTarget>(DateTime time);

        CallTrace? GetNearestTraceUpTo<TTarget>(DateTime time);

        IEnumerable<CallTrace> GetTraces<TTarget>();

        IEnumerable<CallTrace> GetTraces(Version assemblyVersion);
    }

    public sealed class DatabaseCallTraceWriter(IDbContextFactory<CallTracerDbContext> factory) : ICallTraceReader, ICallTraceWriter
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
                .Where(t => t.AssemblyVersion == version && t.AssemblyQualifiedName == name && t.Created <= time)
                .FirstOrDefault();

            if (trace is null)
            {
                return null;
            }

            var result = new CallTrace(
                assemblyVersion: Version.Parse(trace.AssemblyVersion),
                assemblyQualifiedName: trace.AssemblyQualifiedName,
                methodSignature: trace.MethodSignature,
                created: trace.Created);

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
