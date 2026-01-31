using System.Text.Json.Serialization;

namespace NativeAotChecker.Endpoints;

// ============================================================================
// GENERIC PROCESSOR AOT TEST
// ============================================================================
// This test validates that OPEN GENERIC pre/post processors work in AOT mode.
// 
// WITHOUT the source generator (PR1):
//   - The runtime uses Activator.CreateInstance to instantiate 
//     AotGenericPreProcessor<GenericProcessorRequest> from the open generic type
//   - In AOT mode, this FAILS because MakeGenericType + CreateInstance is not supported
//   - The endpoint will NOT run the processor and fail silently or throw
//
// WITH the source generator (PR1):
//   - The generator creates factories like:
//     { (typeof(AotGenericPreProcessor<>), typeof(GenericProcessorRequest)), 
//       () => new AotGenericPreProcessor<GenericProcessorRequest>() }
//   - These factories are registered in GenericTypeRegistryProvider
//   - At runtime, TryCreateClosedPreProcessor finds and calls the factory
//   - The processor runs successfully, setting PreProcessorRan = true
//
// TEST VALIDATION:
//   - If PreProcessorRan == true → Source generator factory worked correctly
//   - If PreProcessorRan == false → Source generator is missing/broken
// ============================================================================

public sealed class GenericProcessorRequest
{
    public string Input { get; set; } = "";
    
    // These flags are set by the processors to prove they ran
    public bool PreProcessorRan { get; set; }
    public bool PostProcessorRan { get; set; }
}

public sealed class GenericProcessorResponse
{
    public string Output { get; set; } = "";
    public bool PreProcessorRan { get; set; }
    public bool PostProcessorRan { get; set; }
}

/// <summary>
/// A generic pre-processor that works with any request type.
/// In AOT mode, this requires factory-based instantiation via source generation.
/// The source generator must detect this open generic and generate:
///   () => new AotGenericPreProcessor&lt;GenericProcessorRequest&gt;()
/// </summary>
public sealed class AotGenericPreProcessor<TReq> : IPreProcessor<TReq>
{
    public Task PreProcessAsync(IPreProcessorContext<TReq> ctx, CancellationToken ct)
    {
        if (ctx.Request is GenericProcessorRequest r)
            r.PreProcessorRan = true;
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// A generic post-processor that works with any request/response type.
/// In AOT mode, this requires factory-based instantiation via source generation.
/// The source generator must detect this open generic and generate:
///   () => new AotGenericPostProcessor&lt;GenericProcessorRequest, GenericProcessorResponse&gt;()
/// 
/// Note: Post-processors run AFTER Send() so they cannot modify the response body.
/// We use a static flag to prove the post-processor ran, which the test can check.
/// </summary>
public sealed class AotGenericPostProcessor<TReq, TRes> : IPostProcessor<TReq, TRes>
{
    // Static flag to verify post-processor execution from tests
    // This is reset before each test and set to true when PostProcessAsync runs
    public static bool DidRun { get; set; }
    
    public Task PostProcessAsync(IPostProcessorContext<TReq, TRes> ctx, CancellationToken ct)
    {
        // Set static flag to prove the post-processor ran
        DidRun = true;
        
        return Task.CompletedTask;
    }
}

public sealed class GenericProcessorEndpoint : Endpoint<GenericProcessorRequest, GenericProcessorResponse>
{
    public override void Configure()
    {
        Post("generic-processor");
        AllowAnonymous();
        SerializerContext<GenericProcessorSerCtx>();
        
        // This is the critical AOT test - passing open generic types
        // These will be closed to:
        //   AotGenericPreProcessor<GenericProcessorRequest>
        //   AotGenericPostProcessor<GenericProcessorRequest, GenericProcessorResponse>
        // In AOT without source generator factories, this FAILS at runtime
        Definition.PreProcessors(Order.Before, typeof(AotGenericPreProcessor<>));
        Definition.PostProcessors(Order.After, typeof(AotGenericPostProcessor<,>));
    }

    public override async Task HandleAsync(GenericProcessorRequest r, CancellationToken c)
    {
        // Return the processor flags in the response to verify they ran
        await Send.OkAsync(new GenericProcessorResponse
        {
            Output = $"Processed: {r.Input}",
            PreProcessorRan = r.PreProcessorRan,
            PostProcessorRan = r.PostProcessorRan
        });
    }
}

/// <summary>
/// Endpoint to verify that the generic post-processor ran.
/// The test calls this AFTER calling /generic-processor to check the static flag.
/// </summary>
public sealed class VerifyPostProcessorEndpoint : EndpointWithoutRequest<VerifyPostProcessorResponse>
{
    public override void Configure()
    {
        Get("verify-post-processor");
        AllowAnonymous();
        SerializerContext<VerifyPostProcessorSerCtx>();
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var didRun = AotGenericPostProcessor<GenericProcessorRequest, GenericProcessorResponse>.DidRun;
        
        // Reset the flag for the next test
        AotGenericPostProcessor<GenericProcessorRequest, GenericProcessorResponse>.DidRun = false;
        
        await Send.OkAsync(new VerifyPostProcessorResponse { PostProcessorRan = didRun });
    }
}

public sealed class VerifyPostProcessorResponse
{
    public bool PostProcessorRan { get; set; }
}

[JsonSerializable(typeof(GenericProcessorRequest))]
[JsonSerializable(typeof(GenericProcessorResponse))]
public partial class GenericProcessorSerCtx : JsonSerializerContext;

[JsonSerializable(typeof(VerifyPostProcessorResponse))]
public partial class VerifyPostProcessorSerCtx : JsonSerializerContext;
