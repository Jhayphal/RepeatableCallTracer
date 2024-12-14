using System.Reflection;
using System.Runtime.CompilerServices;

using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;

namespace RepeatableCallTracer.Tests;

public partial class WithoutExternalDependenciesTest
{
    internal interface ISumBusinessLogic
    {
        int Calculate(int a, int b);
    }

    internal sealed class SumBusinessLogic : ISumBusinessLogic
    {
        public int Calculate(int a, int b)
        {
            return a + b;
        }
    }

    internal sealed class SumBusinessLogicTracer(
        SumBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider)
        : TracedTarget<ISumBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, new CallTracerOptions()), ISumBusinessLogic
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Calculate(int a, int b)
        {
            using var scope = BeginOperation(MethodBase.GetCurrentMethod()!);

            a = scope.SetParameter(nameof(a), a);
            b = scope.SetParameter(nameof(b), b);

            return Target.Calculate(a, b);
        }
    }

    [Fact]
    public void Test_PrimitiveTypesArgs_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        
        SumBusinessLogic target = new();
        SumBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider);

        var expectedA = 1;
        var expectedB = 2;

        var actualForwardCallResult = tracer.Calculate(expectedA, expectedB);
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISumBusinessLogic), trace);

        var actualDebugCallResult = tracer.Calculate(0, 0);
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);

        var actualA = trace.GetTargetMethodParameter<int>("a");
        Assert.Equal(actualA, expectedA);

        var actualB = trace.GetTargetMethodParameter<int>("b");
        Assert.Equal(actualB, expectedB);
    }
}
