using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class WithoutExternalDependenciesTest
{
    internal interface ISumBusinessLogic
    {
        int Calculate(int a, int b);

        int Get(int x);
    }

    internal sealed class SumBusinessLogic : ISumBusinessLogic
    {
        public int Calculate(int a, int b)
            => a + b;

        public int Get(int x)
            => x;
    }

    internal sealed class SumBusinessLogicTracer(
        SumBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider)
        : TracedTarget<ISumBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict), ISumBusinessLogic
    {
        public int Calculate(int a, int b)
        {
            using var scope = BeginOperation(() => Calculate(a, b));

            a = scope.SetParameter(nameof(a), a);
            b = scope.SetParameter(nameof(b), b);

            return Target.Calculate(a, b);
        }

        public int Get(int x)
        {
            using var scope = BeginOperation(() => Get(x));

            x = scope.SetParameter(nameof(x), x);

            return Target.Get(x);
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

    [Fact]
    public void Test_FewMethodsDebugging_Successful()
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


        var expectedX = -1;

        actualForwardCallResult = tracer.Get(expectedX);
        Assert.Equal(2, callTraceWriter.Traces.Count);

        trace = callTraceWriter.Traces.Last!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISumBusinessLogic), trace);

        actualDebugCallResult = tracer.Get(0);
        Assert.Equal(expectedX, actualDebugCallResult);

        var actualX = trace.GetTargetMethodParameter<int>("x");
        Assert.Equal(expectedX, actualX);


        debugCallTraceProvider.DisableDebug(typeof(ISumBusinessLogic));
        actualDebugCallResult = tracer.Get(0);
        Assert.Equal(0, actualDebugCallResult);
    }
}
