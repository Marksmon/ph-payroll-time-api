using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using PhPayrollTimeApi.Api.Constants;

namespace PhPayrollTimeApi.Api.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    private static bool RequiresIdempotencyKey(string path) =>
        path.EndsWith("/logs", StringComparison.OrdinalIgnoreCase) ||
        path.EndsWith("/commit", StringComparison.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method) ||
            !RequiresIdempotencyKey(context.Request.Path.Value ?? ""))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var keyValue))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                type = ProblemTypes.Validation,
                title = "Missing Idempotency-Key",
                status = 400,
                detail = "POST requests to this endpoint require an Idempotency-Key header."
            }, options: null, contentType: "application/problem+json");
            return;
        }

        var cacheKey = ComputeCacheKey(context.Request.Method, context.Request.Path, keyValue!);

        if (_cache.TryGetValue(cacheKey, out IdempotencyCacheEntry? cached) && cached is not null)
        {
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            await context.Response.WriteAsync(cached.Body);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);
        }
        catch
        {
            context.Response.Body = originalBody;
            throw;
        }

        buffer.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(buffer).ReadToEndAsync();

        _cache.Set(cacheKey, new IdempotencyCacheEntry(context.Response.StatusCode, context.Response.ContentType ?? "", body),
            new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });

        buffer.Seek(0, SeekOrigin.Begin);
        await buffer.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }

    public static string ComputeCacheKey(string method, string path, string idempotencyKey)
    {
        var input = $"{method}|{path}|{idempotencyKey}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash);
    }
}

internal record IdempotencyCacheEntry(int StatusCode, string ContentType, string Body);
