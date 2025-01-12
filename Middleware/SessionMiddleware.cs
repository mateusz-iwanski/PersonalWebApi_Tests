using Microsoft.AspNetCore.Http;

public class SessionMiddleware
{
    private readonly RequestDelegate _next;

    public SessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Items["sessionId"] = Guid.NewGuid().ToString();
        context.Items["conversationId"] = Guid.NewGuid().ToString();

        await _next(context);
    }
}
