namespace DnsZoneManager.Services;

/// <summary>
/// Wraps a service call's outcome so the API layer can turn a failure into a friendly
/// 400/404 response with human-readable messages, instead of throwing exceptions for
/// ordinary validation failures (which are expected, not exceptional, in a CRUD app).
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = new();

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(params string[] errors) =>
        new() { Success = false, Errors = errors.ToList() };
}
