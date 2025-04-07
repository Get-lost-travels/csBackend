using System.Reflection;

namespace WebApplication2.extensions;

public static class RoutesExtensions
{
    public static WebApplication MapRoutesFromDirectory(this WebApplication app,
        string routesDirectory)
    {
        if (!Directory.Exists(routesDirectory))
        {
            Directory.CreateDirectory(routesDirectory);
            return app;
        }

        foreach (var file in Directory.GetFiles(routesDirectory, "*.cs",
                     SearchOption.AllDirectories))
        {
            var routePath = GetRoutePath(file, routesDirectory);
            var className = Path.GetFileNameWithoutExtension(file);
            var assembly = Assembly.GetEntryAssembly();
            var routeType = assembly?.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(className));

            if (routeType == null)
            {
                Console.WriteLine(
                    $"Route not registered: {routePath} - Type {className} not found in assembly");
                continue;
            }

            var registerMethod = routeType.GetMethod("RegisterRoutes",
                BindingFlags.Public | BindingFlags.Static);

            if (registerMethod == null)
            {
                Console.WriteLine($"Route not registered: {routePath} - Missing RegisterRoutes method in {className}");
                continue;
            }

            registerMethod.Invoke(null, new object[] { app, routePath });
        }

        return app;
    }

    private static string GetRoutePath(string filePath, string baseDirectory)
    {
        var relativePath = filePath.Substring(baseDirectory.Length)
            .ToLower()
            .Replace("\\", "/")
            .TrimStart('/')
            .Replace(".cs", "")
            .Replace("/routes", "/");

        return relativePath;
    }
}