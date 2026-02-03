using System.Net;
using NativeAotChecker.Endpoints;

namespace NativeAotCheckerTests;

public class NullableEnumQueryBindingTests(App app)
{
    /// <summary>
    /// Tests nullable enum (OrderStatus?) query parameter binding in AOT mode.
    /// 
    /// AOT ISSUE: Nullable enum binding from query string may fail in Native AOT.
    /// - Non-nullable enum query param works: ?Status=Shipped → 200 OK
    /// - Nullable enum? query param may fail: ?NullableStatus=Shipped → 500 Error
    /// 
    /// The issue may be similar to nullable primitive types - parsing nullable 
    /// enum types from query strings in AOT mode.
    /// </summary>
    [Fact]
    public async Task Non_Nullable_Enum_Query_Binding_Works()
    {
        // This should work - non-nullable enum
        var rsp = await app.Client.GetAsync("nullable-enum-query-test?Status=Shipped");
        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Nullable_Enum_Query_Binding_With_Value()
    {
        // This may fail in AOT - nullable enum with value
        var (rsp, res, err) = await app.Client.GETAsync<NullableEnumQueryEndpoint, NullableEnumQueryRequest, NullableEnumQueryResponse>(
            new()
            {
                Status = OrderStatus.Processing,
                NullableStatus = OrderStatus.Shipped
            });

        if (!rsp.IsSuccessStatusCode)
            Assert.Fail($"Nullable enum query binding failed with status {rsp.StatusCode}. " +
                        $"Non-nullable enum works, but OrderStatus? fails in AOT mode. Error: {err}");

        res.Status.ShouldBe(OrderStatus.Processing);
        res.NullableStatus.ShouldBe(OrderStatus.Shipped);
    }

    [Fact]
    public async Task Nullable_Enum_Query_Binding_With_Null()
    {
        // Test nullable enum when not provided (should be null)
        var (rsp, res, err) = await app.Client.GETAsync<NullableEnumQueryEndpoint, NullableEnumQueryRequest, NullableEnumQueryResponse>(
            new()
            {
                Status = OrderStatus.Pending
                // NullableStatus not provided - should be null
            });

        if (!rsp.IsSuccessStatusCode)
            Assert.Fail($"Nullable enum query binding failed: {err}");

        res.Status.ShouldBe(OrderStatus.Pending);
        res.NullableStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Multiple_Nullable_Enums_Together()
    {
        var (rsp, res, err) = await app.Client.GETAsync<NullableEnumQueryEndpoint, NullableEnumQueryRequest, NullableEnumQueryResponse>(
            new()
            {
                Status = OrderStatus.Delivered,
                NullableStatus = OrderStatus.Cancelled,
                NullablePriority = Priority.High
            });

        if (!rsp.IsSuccessStatusCode)
            Assert.Fail($"Multiple nullable enum binding failed: {err}");

        res.Status.ShouldBe(OrderStatus.Delivered);
        res.NullableStatus.ShouldBe(OrderStatus.Cancelled);
        res.NullablePriority.ShouldBe(Priority.High);
        res.NullablePermissions.ShouldBeNull();
    }

    [Fact]
    public async Task Nullable_Flags_Enum_Query_Binding()
    {
        var (rsp, res, err) = await app.Client.GETAsync<NullableEnumQueryEndpoint, NullableEnumQueryRequest, NullableEnumQueryResponse>(
            new()
            {
                Status = OrderStatus.Pending,
                NullablePermissions = Permissions.Read | Permissions.Write
            });

        if (!rsp.IsSuccessStatusCode)
            Assert.Fail($"Nullable flags enum binding failed: {err}");

        res.NullablePermissions.ShouldBe(Permissions.Read | Permissions.Write);
    }

    [Fact]
    public async Task Nullable_Priority_Enum_All_Values()
    {
        // Test each priority value
        foreach (var priority in Enum.GetValues<Priority>())
        {
            var (rsp, res, err) = await app.Client.GETAsync<NullableEnumQueryEndpoint, NullableEnumQueryRequest, NullableEnumQueryResponse>(
                new()
                {
                    Status = OrderStatus.Pending,
                    NullablePriority = priority
                });

            if (!rsp.IsSuccessStatusCode)
                Assert.Fail($"Nullable Priority enum binding failed for value '{priority}': {err}");

            res.NullablePriority.ShouldBe(priority);
        }
    }
}
