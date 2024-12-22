using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class AsyncTargetMethodTest
{
    internal interface ISomeBusinessLogic
    {
        Task<int> CalculateAsync(int a, int b);

        Task<int> PowAsync(int x);
    }

    internal sealed class SomeBusinessLogic : ISomeBusinessLogic
    {
        public async Task<int> CalculateAsync(int a, int b)
        {
            await Task.Delay(10);

            return a + b;
        }

        public Task<int> PowAsync(int x)
        {
            throw new DivideByZeroException();
        }
    }

    internal sealed class SomeBusinessLogicTracer(
        SomeBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider)
        : TracedTarget<ISomeBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict), ISomeBusinessLogic
    {
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

        public async Task<int> PowAsync(int x)
        {
            var scope = BeginOperation(() => PowAsync(x));

            try
            {
                scope.SetParameter(nameof(x), ref x);

                return await Target.PowAsync(x);
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

        Assert.True(trace.Elapsed > TimeSpan.Zero, "Time was not recorded.");
    }

    [Fact]
    public async Task Test_AsyncMethod_ErrorCapturing_Failed()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();

        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider);

        var expectedX = 1;

        await Assert.ThrowsAsync<DivideByZeroException>(async () => await tracer.PowAsync(expectedX));
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        Assert.False(string.IsNullOrEmpty(trace.Error), "Error was not captured.");
    }
}
