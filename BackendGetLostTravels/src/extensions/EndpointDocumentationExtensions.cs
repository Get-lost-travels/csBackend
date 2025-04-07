using System.Text.Json;
using Microsoft.OpenApi.Any;

namespace WebApplication2.extensions;

public static class EndpointDocumentationExtensions
{
    // Add JSON response types for multiple status codes
    public static RouteHandlerBuilder WithJsonResponseTypes(this RouteHandlerBuilder builder, params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            if (statusCode != StatusCodes.Status204NoContent && statusCode != StatusCodes.Status401Unauthorized)
                builder.Produces(statusCode, typeof(object), "application/json");
            else 
                builder.Produces(statusCode);
        }
        return builder;
    }

    // Add a response example for a specific status code
    public static RouteHandlerBuilder WithResponseExample(
        this RouteHandlerBuilder builder,
        int statusCode,
        object example)
    {
        builder.WithOpenApi(operation =>
        {
            var statusCodeStr = statusCode.ToString();
            if (operation.Responses.TryGetValue(statusCodeStr, out var response))
            {
                if (response.Content.TryGetValue("application/json", out var mediaType))
                {
                    var exampleJson = JsonSerializer.Serialize(example);
                    using var doc = JsonDocument.Parse(exampleJson);
                    mediaType.Example = ConvertJsonElementToOpenApiAny(doc.RootElement);
                }
            }
            return operation;
        });

        return builder;
    }

    public static RouteHandlerBuilder WithApiDocumentation(
        this RouteHandlerBuilder builder,
        string operationName,
        string tag,
        string summary,
        string description,
        params int[] statusCodes)
    {
        return builder
            .WithName(operationName)
            .WithTags(tag)
            .WithSummary(summary)
            .WithDescription(description)
            .WithOpenApi()
            .WithJsonResponseTypes(statusCodes);
    }
    
    // Helper method to convert JsonElement to IOpenApiAny
    private static IOpenApiAny ConvertJsonElementToOpenApiAny(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new OpenApiObject();
                foreach (var property in element.EnumerateObject())
                {
                    obj.Add(property.Name, ConvertJsonElementToOpenApiAny(property.Value));
                }
                return obj;
            case JsonValueKind.Array:
                var array = new OpenApiArray();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(ConvertJsonElementToOpenApiAny(item));
                }
                return array;
            case JsonValueKind.String:
                return new OpenApiString(element.GetString());
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return new OpenApiInteger(intValue);
                return new OpenApiDouble(element.GetDouble());
            case JsonValueKind.True:
                return new OpenApiBoolean(true);
            case JsonValueKind.False:
                return new OpenApiBoolean(false);
            case JsonValueKind.Null:
                return new OpenApiNull();
            default:
                return new OpenApiString(element.ToString());
        }
    }
}