using System.Net.Http.Json;
using NativeAotChecker.Endpoints.KnownAotIssues;

namespace NativeAotCheckerTests;

public class ResponseCachingTest(App app)
{
    /// <summary>
    /// Tests ResponseCache() configuration in AOT mode.
    /// AOT ISSUE: ResponseCache uses CacheProfile and cache headers set via reflection.
    /// IOutputCachePolicy implementation resolution requires reflection.
    /// Cache key generation may use property reflection for vary-by parameters.
    /// </summary>
    [Fact] // AOT ISSUE: ResponseCache configuration doesn't work in Native AOT mode
    public async Task Response_Caching_Configuration_Works_In_AOT_Mode()
    {
        var (rsp, res, err) = await app.Client.GETAsync<ResponseCachingEndpoint,
CachedResponse>();

        if (!rsp.IsSuccessStatusCode)
            Assert.Fail(err);

        res.Data.ShouldNotBeNullOrEmpty();
        res.UniqueId.ShouldNotBeNullOrEmpty();
    }
}
