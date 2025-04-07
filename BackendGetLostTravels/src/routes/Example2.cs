using WebApplication2.classes;

namespace WebApplication2.routes;

public class Example2
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        // Basic GET endpoint
        app.MapGet($"/{routePath}", () => 
            new { message = "Hello from example route!", path = routePath });
            
        // POST endpoint example
        app.MapPost($"/{routePath}", (ExampleRequest request) => 
            new { message = $"Received: {request.Data}", timestamp = DateTime.UtcNow });
            
        Console.WriteLine($"Route registered successfully: /{routePath}");
    }
}