using System.Reflection;

namespace WebApplication2.extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseMiddlewaresFromDirectory(this WebApplication app, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
            return app;
        }

        var middlewareTypes = Directory.GetFiles(directoryPath, "*.cs")
            .Select(file => {
                var className = Path.GetFileNameWithoutExtension(file);
                var assembly = Assembly.GetEntryAssembly();
                return assembly?.GetTypes().FirstOrDefault(t => t.Name.Equals(className) &&
                    t.Name.EndsWith("Middleware"));
            })
            .Where(t => t != null);

        foreach (var middlewareType in middlewareTypes)
        {
            app.UseMiddleware(middlewareType!);
        }

        return app;
    }
}