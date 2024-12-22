using System.Runtime.CompilerServices;

using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class AsyncTargetMethodTest
{
    internal interface ISomeBusinessLogic
    {
        Task<int> CalculateAsync(int a, int b);
    }

    internal sealed class SomeBusinessLogic : ISomeBusinessLogic
    {
        public async Task<int> CalculateAsync(int a, int b)
        {
            await Task.Delay(10);

            return a + b;
        }
    }

    internal sealed class SomeBusinessLogicTracer(
        SomeBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider)
        : TracedTarget<ISomeBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict), ISomeBusinessLogic
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task<int> CalculateAsync(int a, int b)
        {
            var scope = BeginOperation(() => CalculateAsync(a, b));

            try
            {
                scope.SetParameter(nameof(a), ref a);
                scope.SetParameter(nameof(b), ref b);

                return await Target.CalculateAsync(a, b);
            }
            catch (Exception ex)
            {
                scope.SetError(ex);
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        }
    }

    [Fact]
    public async Task Test_AsyncMethod_PrimitiveTypesArgs_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();

        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider);

        var expectedA = 1;
        var expectedB = 2;

        var actualForwardCallResult = await tracer.CalculateAsync(expectedA, expectedB);
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);

        var actualDebugCallResult = await tracer.CalculateAsync(0, 0);
        Assert.Equal(actualForwardCallResult, actualDebugCallResult);

        var actualA = trace.GetTargetMethodParameter<int>("a");
        Assert.Equal(actualA, expectedA);

        var actualB = trace.GetTargetMethodParameter<int>("b");
        Assert.Equal(actualB, expectedB);
    }
}
