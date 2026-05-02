namespace PhPayrollTimeApi.Api.Middleware;

// Placeholder — domain exception → RFC 7807 mapping implemented in Story 1.3
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context) => _next(context);
}
