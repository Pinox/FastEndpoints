using NativeAotChecker.Endpoints.KnownAotIssues;

namespace NativeAotCheckerTests;

public class FromHeaderBindingTest(App app)
{
    /// <summary>
    /// Tests [FromHeader] attribute binding in AOT mode.
    /// AOT ISSUE: [FromHeader] binding uses reflection to find properties with the attribute.
    /// GetCustomAttribute&lt;FromHeaderAttribute&gt;() scans for metadata that may be trimmed.
    /// Header name-to-property mapping requires runtime reflection.
    /// </summary>
    [Fact]
    public async Task Header_Binding_Works_In_AOT_Mode()
    {
        var correlationId = Guid.NewGuid().ToString();
        var tenantId = "tenant-123";

        app.Client.DefaultRequestHeaders.Add("x-correlation-id", correlationId);
        app.Client.DefaultRequestHeaders.Add("x-tenant-id", tenantId);

        try
        {
            var (rsp, res, err) = await app.Client.GETAsync<FromHeaderBindingEndpoint, FromHeaderRequest, FromHeaderResponse>(new());

            if (!rsp.IsSuccessStatusCode)
                Assert.Fail(err);

            res.CorrelationId.ShouldBe(correlationId);
            res.TenantId.ShouldBe(tenantId);
            res.AllHeadersBound.ShouldBeTrue();
        }
        finally
        {
            app.Client.DefaultRequestHeaders.Remove("x-correlation-id");
            app.Client.DefaultRequestHeaders.Remove("x-tenant-id");
        }
    }
}
