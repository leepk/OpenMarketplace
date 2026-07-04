namespace OpenMarketplace.Shared.Api;

public sealed record ApiResponse<T>(bool Success, T? Data, ApiError? Error, string? TraceId)
{
    public static ApiResponse<T> Ok(T data, string? traceId = null) => new(true, data, null, traceId);
    public static ApiResponse<T> Fail(string code, string message, string? traceId = null) => new(false, default, new ApiError(code, message), traceId);
}
public sealed record ApiError(string Code, string Message);
