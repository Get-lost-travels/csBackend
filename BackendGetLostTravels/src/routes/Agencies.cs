using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;
using WebApplication2.classes.requests.agency;
using WebApplication2.extensions;

namespace WebApplication2.routes;

public class Agencies
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        app.MapPost($"/{routePath}/create",
            async ([FromBody] AgencyRequest request, DatabaseContext db) =>
            {
                if (string.IsNullOrEmpty(request.Name))
                {
                    return Results.BadRequest(new
                    {
                        status = 400,
                        message = "Agency name is required"
                    });
                }

                var agency = new classes.entities.Agency
                {
                    UserId = request.UserId,
                    Name = request.Name,
                    BannerUrl = request.BannerUrl,
                    LogoUrl = request.LogoUrl,
                    Address = request.Address,
                    Phone = request.Phone,
                };

                db.Agencies.Add(agency);
                await db.SaveChangesAsync();

                return Results.Created($"/{routePath}/{agency.Id}", new
                {
                    status = 201,
                    message = "Agency created successfully",
                    agency
                });
            })
            .WithApiDocumentation("Create agency", "Agencies", "Creates a new agency",
                "Adds a new agency to the system (without analytics, messages, services, or user).",
                StatusCodes.Status201Created, StatusCodes.Status400BadRequest)
            .WithResponseExample(StatusCodes.Status201Created, new
            {
                status = 201,
                message = "Agency created successfully",
                agency = new
                {
                    Id = 1,
                    Name = "My Agency",
                    UserId = (int?)null,
                    BannerUrl = "https://example.com/banner.jpg",
                    LogoUrl = "https://example.com/logo.jpg",
                    Address = "123 Main St",
                    Phone = "555-1234"
                }
            });

        app.MapGet($"/{routePath}/{{id}}",
            async (int id, DatabaseContext db) =>
            {
                var agency = await db.Agencies
                    .FirstOrDefaultAsync(a => a.Id == id);

                return agency == null
                    ? Results.NotFound(new { status = 404, message = "Agency not found" })
                    : Results.Ok(new { status = 200, agency });
            })
            .WithApiDocumentation("Get agency", "Agencies", "Get an agency by ID",
                "Fetches a single agency and its user if available.",
                StatusCodes.Status200OK, StatusCodes.Status404NotFound);

        app.MapPut($"/{routePath}/{{id}}",
            async (int id, [FromBody] AgencyRequest request, DatabaseContext db) =>
            {
                var agency = await db.Agencies.FindAsync(id);
                if (agency == null)
                    return Results.NotFound(new { status = 404, message = "Agency not found" });

                agency.Name = request.Name;
                agency.UserId = request.UserId;
                agency.BannerUrl = request.BannerUrl;
                agency.LogoUrl = request.LogoUrl;
                agency.Address = request.Address;
                agency.Phone = request.Phone;

                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    status = 200,
                    message = "Agency updated successfully",
                    agency
                });
            })
            .WithApiDocumentation("Update agency", "Agencies", "Updates an agency by ID",
                "Modifies the details of an existing agency.",
                StatusCodes.Status200OK, StatusCodes.Status404NotFound);

        app.MapDelete($"/{routePath}/{{id}}",
            async (int id, DatabaseContext db) =>
            {
                var agency = await db.Agencies.FindAsync(id);
                if (agency == null)
                    return Results.NotFound(new { status = 404, message = "Agency not found" });

                db.Agencies.Remove(agency);
                await db.SaveChangesAsync();

                return Results.Ok(new { status = 200, message = "Agency deleted successfully" });
            })
            .WithApiDocumentation("Delete agency", "Agencies", "Deletes an agency",
                "Removes the agency from the system by its ID.",
                StatusCodes.Status200OK, StatusCodes.Status404NotFound);
        
        app.MapGet($"/{routePath}", async ([FromQuery] string? search, DatabaseContext db) =>
            {
                var query = db.Agencies
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(a => a.Name.ToLower().Contains(search.ToLower()));
                }

                var agencies = await query
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        a.UserId,
                        a.BannerUrl,
                        a.LogoUrl,
                        a.Address,
                        a.Phone
                    })
                    .ToListAsync();

                return Results.Ok(new
                {
                    status = 200,
                    count = agencies.Count,
                    agencies
                });
            })
            .WithApiDocumentation("List agencies", "Agencies", "List all agencies with optional search",
                "Returns all agencies or filters them by name using the `search` query parameter.",
                StatusCodes.Status200OK)
            .WithResponseExample(StatusCodes.Status200OK, new
            {
                status = 200,
                count = 1,
                agencies = new[]
                {
                    new
                    {
                        Id = 1,
                        Name = "My Agency",
                        UserId = (int?)null,
                        BannerUrl = "https://example.com/banner.jpg",
                        LogoUrl = "https://example.com/logo.jpg",
                        Address = "123 Main St",
                        Phone = "555-1234"
                    }
                }
            });
    }
}
