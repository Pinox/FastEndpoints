using System.Text.Json.Serialization;

namespace NativeAotChecker.Endpoints;

/// <summary>
/// Test: Nullable enum query parameter binding in AOT mode.
/// AOT ISSUE: Nullable enum (MyEnum?) binding from query string may fail.
/// Non-nullable enum works fine, but MyEnum? may cause issues similar to bool?.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

public enum Priority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

[Flags]
public enum Permissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    Admin = 8
}

public sealed class NullableEnumQueryRequest
{
    // Non-nullable enum (should work)
    [QueryParam]
    public OrderStatus Status { get; set; }

    // Nullable enum (potential AOT issue)
    [QueryParam]
    public OrderStatus? NullableStatus { get; set; }

    // Another nullable enum
    [QueryParam]
    public Priority? NullablePriority { get; set; }

    // Flags enum nullable
    [QueryParam]
    public Permissions? NullablePermissions { get; set; }
}

public sealed class NullableEnumQueryResponse
{
    public OrderStatus Status { get; set; }
    public OrderStatus? NullableStatus { get; set; }
    public Priority? NullablePriority { get; set; }
    public Permissions? NullablePermissions { get; set; }
}

public sealed class NullableEnumQueryEndpoint : Endpoint<NullableEnumQueryRequest, NullableEnumQueryResponse>
{
    public override void Configure()
    {
        Get("nullable-enum-query-test");
        AllowAnonymous();
        SerializerContext<NullableEnumQuerySerCtx>();
    }

    public override async Task HandleAsync(NullableEnumQueryRequest req, CancellationToken ct)
    {
        await Send.OkAsync(new NullableEnumQueryResponse
        {
            Status = req.Status,
            NullableStatus = req.NullableStatus,
            NullablePriority = req.NullablePriority,
            NullablePermissions = req.NullablePermissions
        });
    }
}

[JsonSerializable(typeof(NullableEnumQueryRequest))]
[JsonSerializable(typeof(NullableEnumQueryResponse))]
public partial class NullableEnumQuerySerCtx : JsonSerializerContext;
