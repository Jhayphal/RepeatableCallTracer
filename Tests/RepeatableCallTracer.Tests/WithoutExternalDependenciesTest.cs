using RepeatableCallTracer.Debuggers;
using RepeatableCallTracer.Targets;
using RepeatableCallTracer.Tests.Infrastructure;

namespace RepeatableCallTracer.Tests;

public partial class WithoutExternalDependenciesTest
{
    internal interface ISomeBusinessLogic
    {
        int Calculate(int a, int b);

        int Get(int x);

        int Sub(int a, int b);
    }

    internal sealed class SomeBusinessLogic : ISomeBusinessLogic
    {
        public int Calculate(int a, int b)
            => a + b;

        public int Get(int x)
            => x;

        public int Sub(int a, int b)
            => a - b;
    }

    internal sealed class SomeBusinessLogicTracer(
        SomeBusinessLogic target,
        ICallTraceWriter callTraceWriter,
        IDebugCallTraceProvider debugCallTraceProvider,
        CallTracerOptions options)
        : TracedTarget<ISomeBusinessLogic>(target, callTraceWriter, debugCallTraceProvider, options), ISomeBusinessLogic
    {
        public int Calculate(int a, int b)
        {
            var scope = BeginOperation(() => Calculate(a, b));

            try
            {
                scope.SetParameter(nameof(a), ref a);
                scope.SetParameter(nameof(b), ref b);

                return Target.Calculate(a, b);
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

        public int Get(int x)
        {
            var scope = BeginOperation(() => Get(x));

            try
            {
                scope.SetParameter(nameof(x), ref x);

                return Target.Get(x);
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

        public int Sub(int a, int b)
        {
            var scope = BeginOperation(() => Sub(a, b));

            try
            {
                scope.SetParameter(nameof(a), ref a);
                // scope.SetParameter(nameof(b), ref b);

                return Target.Sub(a, b);
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
    public void Test_PrimitiveTypesArgs_Successful()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        
        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict);

        var expectedA = 1;
        var expectedB = 2;

        var actualForwardCallResult = tracer.Calculate(expectedA, expectedB);
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);
        
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

        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, CallTracerOptions.Strict);

        var expectedA = 1;
        var expectedB = 2;

        var actualForwardCallResult = tracer.Calculate(expectedA, expectedB);
        Assert.Single(callTraceWriter.Traces);

        var trace = callTraceWriter.Traces.First!.Value;
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);

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
        debugCallTraceProvider.EnableDebug(typeof(ISomeBusinessLogic), trace);

        actualDebugCallResult = tracer.Get(0);
        Assert.Equal(expectedX, actualDebugCallResult);

        var actualX = trace.GetTargetMethodParameter<int>("x");
        Assert.Equal(expectedX, actualX);


        debugCallTraceProvider.DisableDebug(typeof(ISomeBusinessLogic));
        actualDebugCallResult = tracer.Get(0);
        Assert.Equal(0, actualDebugCallResult);
    }

    [Fact]
    public void Test_WrongTracerArgs_Failed()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new(
            ThrowIfHasUntrackedDependencies: false,
            ThrowIfParametersDifferMethodSignature: true,
            ThrowIfValueCannotBeDeserializedCorrectly: false);

        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        Assert.Throws<InvalidProgramException>(() => tracer.Sub(0, 0));
        Assert.Empty(callTraceWriter.Traces);
    }

    [Fact]
    public void Test_WrongTracerArgs_Success()
    {
        DebugCallTraceProvider debugCallTraceProvider = new();
        InMemoryCallTraceWriter callTraceWriter = new();
        CallTracerOptions options = new(
            ThrowIfHasUntrackedDependencies: false,
            ThrowIfParametersDifferMethodSignature: false,
            ThrowIfValueCannotBeDeserializedCorrectly: false);

        SomeBusinessLogic target = new();
        SomeBusinessLogicTracer tracer = new(target, callTraceWriter, debugCallTraceProvider, options);

        Assert.Equal(0, tracer.Sub(0, 0));
    }
}
