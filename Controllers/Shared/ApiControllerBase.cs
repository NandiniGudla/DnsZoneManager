using DnsZoneManager.Services;
using Microsoft.AspNetCore.Mvc;

public abstract class ApiControllerBase : ControllerBase
{
    protected abstract string ResourceName { get; }
    /// <summary>
    /// Converts a <see cref="ServiceResult{T}"/> to an <see cref ="IActionResult"/>. If the service result indicates success, it invokes the provided <paramref name="onSuccess"/> function to generate the response. If the service result indicates failure, it returns a 400 or 404 response with the error messages.
    /// </summary>
    /// <typeparam name="T">The type of the data in the service result.</typeparam>
    /// <param name="result">The service result to convert.</param>
    /// <param name="onSuccess">The function to invoke if the service result indicates success.</param>
    /// <returns>The<IActionResult/> representing the HTTP response.</returns>
    protected IActionResult ToActionResult<T>(ServiceResult<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.Success) return onSuccess(result.Data!);
        var isNotFound = result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase));
        var errors = result.Errors.Count > 0 ? result.Errors : new List<string> { $"The requested {ResourceName} could not be processed." };
        return StatusCode(isNotFound ? 404 : 400, new { errors });
    }
}