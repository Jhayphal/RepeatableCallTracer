using System.Reflection;
using System.Runtime.CompilerServices;

using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;

namespace RepeatableCallTracer.Tests;

public partial class AsyncTargetMethodTest
{
    internal interface ISumBusinessLogic
    {
        Task<int> CalculateAsync(int a, int b);
    }

    internal sealed class SumBusinessLogic : ISumBusinessLogic
    {
        public async Task<int> CalculateAsync(int a, int b)
        {
            await Task.Delay(10);

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
        public async Task<int> CalculateAsync(int a, int b)
        {
            using var scope = BeginOperation(MethodBase.GetCurrentMethod()!);

            a = scope.SetParameter(nameof(a), a);
            b = scope.SetParameter(nameof(b), b);

            return await Target.CalculateAsync(a, b);
        }
    }

    [Fact]
    public async Task Test_AsyncMethod_PrimitiveTypesArgs_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();

        SumBusinessLogic target = new();
        SumBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider);

        var expectedA = 1;
        var expectedB = 2;

        var actualForwardCallResult = await tracer.CalculateAsync(expectedA, expectedB);
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISumBusinessLogic), trace);

        var actualDebugCallResult = await tracer.CalculateAsync(0, 0);
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);

        var actualA = trace.GetTargetMethodParameter<int>("a");
        Assert.Equal(actualA, expectedA);

        var actualB = trace.GetTargetMethodParameter<int>("b");
        Assert.Equal(actualB, expectedB);
    }
}
