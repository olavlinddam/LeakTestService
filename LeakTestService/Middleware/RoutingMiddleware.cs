namespace LeakTestService.Middleware;

public class RoutingMiddleware
{
    private readonly RequestDelegate _next;

    public RoutingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method == "GET")
        {
            // Read identifier from the context
            var identifier = context.Request.Query["identifier"].ToString();

            // determine route based on the identifier
            context.Request.Path = identifier switch
            {
                "type1" => "/api/LeakTests/Type1Endpoint",
                "type2" => "/api/LeakTests/Type2Endpoint",
                _ => context.Request.Path
            };
        }

        // Fortsæt med næste middleware/endpoint
        await _next(context);
    }
}