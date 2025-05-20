using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;
using WebApplication2.classes.requests.services;
using WebApplication2.extensions;

namespace WebApplication2.routes;

public class Services
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        app.MapGet($"/{routePath}/paged-filtered", async (
            DatabaseContext db,
            [FromQuery] int pageNumber = 1,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int? categoryFilter = null,
            [FromQuery] int? agencyFilter = null
            ) =>
        {
            int pageSize = 20;

            if (pageNumber < 1 || pageSize < 1)
            {
                return Results.BadRequest(new { status = 400, message = "Invalid page number." });
            }

            var query = db.Services
                .Include(s => s.Category)
                .Include(s => s.Agency)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(s =>
                    s.Title.Contains(searchTerm) ||
                    (s.Category != null && s.Category.Name.Contains(searchTerm)));
            }

            if (categoryFilter.HasValue)
            {
                query = query.Where(s => s.CategoryId == categoryFilter.Value);
            }

            if (agencyFilter.HasValue)
            {
                query = query.Where(s => s.AgencyId == agencyFilter.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var services = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.AgencyId,
                    AgencyName = s.Agency != null ? s.Agency.Name : null,
                    s.CategoryId,
                    CategoryName = s.Category != null ? s.Category.Name : null,
                    s.Title,
                    s.Price,
                    s.Location,
                    s.Duration,
                    s.Description
                })
                .ToListAsync();

            return Results.Ok(new
            {
                status = 200,
                pageNumber,
                pageSize,
                totalCount,
                totalPages,
                services
            });
        })
        .WithTags("Services")
        .WithApiDocumentation("List services paginated and filterd", "Services", "Get services with pagination, search, and filtering", "Retrieves services with pagination, optional search by name or category, and filtering by category or agency.",
            StatusCodes.Status200OK, StatusCodes.Status400BadRequest)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            pageNumber = 1,
            pageSize = 20,
            totalCount = 50,
            totalPages = 3,
            services = new[]
            {
                new { Id = 1, AgencyId = 1, AgencyName = "Travel Agency A", CategoryId = 2, CategoryName = "Tours", Title = "City Tour", Price = 50.00, Location = "Cluj-Napoca", Duration = "4 hours", Description = "A guided tour of the city center." },
            }
        })
        .WithResponseExample(StatusCodes.Status400BadRequest, new { status = 400, message = "Invalid page number." });

        app.MapPost($"/{routePath}/create", async (
            [FromBody] CreateServiceRequest request,
            DatabaseContext db) =>
        {
            var agency = await db.Agencies.FindAsync(request.AgencyId);
            var category = await db.ServiceCategories.FindAsync(request.CategoryId);

            if (agency == null)
                return Results.NotFound(new { status = 404, message = "Agency not found" });

            if (category == null)
                return Results.NotFound(new { status = 404, message = "Category not found" });

            if (await db.Services.AnyAsync(
                s => s.AgencyId == request.AgencyId &&
                     s.CategoryId == request.CategoryId &&
                     s.Title == request.Title))
            {
                return Results.Conflict(new
                {
                    status = 409,
                    message = "A service with the same Agency, Category, and Title already exists."
                });
            }

            var service = new Service
            {
                AgencyId = request.AgencyId,
                CategoryId = request.CategoryId,
                Title = request.Title,
                Price = request.Price,
                Location = request.Location,
                Duration = request.Duration,
                Description = request.Description,
                Itinerary = request.Itinerary,
                Inclusions = request.Inclusions,
                Exclusions = request.Exclusions,
                Terms = request.Terms
            };

            db.Services.Add(service);
            await db.SaveChangesAsync();

            return Results.Created($"/{routePath}/{service.Id}", new
            {
                status = 201,
                message = "Service created successfully",
                service = new
                {
                    service.Id,
                    service.AgencyId,
                    service.CategoryId,
                    service.Title,
                    service.Price,
                    service.Location,
                    service.Duration,
                    service.Description,
                    service.Itinerary,
                    service.Inclusions,
                    service.Exclusions,
                    service.Terms
                }
            });
        })
        .WithApiDocumentation("Create service", "Services", "Creates a new service",
            "Adds a new service to the system, ensuring uniqueness based on Agency, Category, and Title.",
            StatusCodes.Status201Created, StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict) // Added 409 Conflict
        .WithResponseExample(StatusCodes.Status201Created, new
        {
            status = 201,
            message = "Service created successfully",
            service = new
            {
                Id = 1,
                AgencyId = 1,
                CategoryId = 1,
                Title = "Amazing Tour",
                Price = 99.99,
                Location = "Paris",
                Duration = "3 days",
                Description = "A fantastic tour of Paris.",
                Itinerary = "Day 1: Eiffel Tower, Day 2: Louvre...",
                Inclusions = "Flights, Hotel",
                Exclusions = "Meals",
                Terms = "Full payment required upfront."
            }
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Agency not found" })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Category not found" })
        .WithResponseExample(StatusCodes.Status409Conflict, new
        {
            status = 409,
            message = "A service with the same Agency, Category, and Title already exists."
        });

        app.MapGet($"/{routePath}", async (DatabaseContext db) =>
        {
            var services = await db.Services
                .Include(s => s.Agency)
                .Include(s => s.Category)
                .Select(s => new
                {
                    s.Id,
                    s.AgencyId,
                    s.CategoryId,
                    s.Title,
                    s.Price,
                    s.Location,
                    s.Duration,
                    s.Description
                })
                .ToListAsync();

            return Results.Ok(new { status = 200, count = services.Count, services });
        })
        .WithApiDocumentation("List services", "Services", "Lists all services",
            "Retrieves a list of all services in the system.",
            StatusCodes.Status200OK)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            count = 2,
            services = new[]
            {
                new
                {
                    Id = 1,
                    AgencyId = 1,
                    CategoryId = 1,
                    Title = "Amazing Tour",
                    Price = 99.99,
                    Location = "Paris",
                    Duration = "3 days",
                    Description = "A fantastic tour of Paris."
                },
                new
                {
                    Id = 2,
                    AgencyId = 1,
                    CategoryId = 2,
                    Title = "Relaxing Spa Day",
                    Price = 75.00,
                    Location = "Local Spa",
                    Duration = "1 day",
                    Description = "Enjoy a full day of relaxation."
                }
            }
        });

        app.MapGet($"/{routePath}/{{id}}", async (int id, DatabaseContext db) =>
        {
            var service = await db.Services
                .Include(s => s.Agency)
                .Include(s => s.Category)
                .FirstOrDefaultAsync(s => s.Id == id);

            return service == null
                ? Results.NotFound(new { status = 404, message = "Service not found" })
                : Results.Ok(new { status = 200, service = new
                {
                    service.Id,
                    service.AgencyId,
                    service.CategoryId,
                    service.Title,
                    service.Price,
                    service.Location,
                    service.Duration,
                    service.Description,
                    service.Itinerary,
                    service.Inclusions,
                    service.Exclusions,
                    service.Terms,
                    Agency = service.Agency == null ? null : new { service.Agency.Id, service.Agency.Name },
                    Category = service.Category == null ? null : new { service.Category.Id, service.Category.Name }
                }});
        })
        .WithApiDocumentation("Get service by ID", "Services", "Retrieves a service by its ID",
            "Fetches a single service and its associated agency and category.",
            StatusCodes.Status200OK, StatusCodes.Status404NotFound)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            service = new
            {
                Id = 1,
                AgencyId = 1,
                CategoryId = 1,
                Title = "Amazing Tour",
                Price = 99.99,
                Location = "Paris",
                Duration = "3 days",
                Description = "A fantastic tour of Paris.",
                Itinerary = "Day 1: Eiffel Tower, Day 2: Louvre...",
                Inclusions = "Flights, Hotel",
                Exclusions = "Meals",
                Terms = "Full payment required upfront.",
                Agency = new { Id = 1, Name = "My Agency" },
                Category = new { Id = 1, Name = "Adventure" }
            }
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Service not found" });

                app.MapPut($"/{routePath}/{{id}}", async (int id, [FromBody] CreateServiceRequest request, DatabaseContext db) =>
        {
            var serviceToUpdate = await db.Services.FindAsync(id);
            if (serviceToUpdate == null)
            {
                return Results.NotFound(new { status = 404, message = "Service not found" });
            }

            var agency = await db.Agencies.FindAsync(request.AgencyId);
            var category = await db.ServiceCategories.FindAsync(request.CategoryId);

            if (agency == null)
                return Results.NotFound(new { status = 404, message = "Agency not found" });

            if (category == null)
                return Results.NotFound(new { status = 404, message = "Category not found" });

            if (await db.Services.AnyAsync(
                s => s.AgencyId == request.AgencyId &&
                     s.CategoryId == request.CategoryId &&
                     s.Title == request.Title &&
                     s.Id != id))
            {
                return Results.Conflict(new
                {
                    status = 409,
                    message = "A service with the same Agency, Category, and Title already exists."
                });
            }

            serviceToUpdate.AgencyId = request.AgencyId;
            serviceToUpdate.CategoryId = request.CategoryId;
            serviceToUpdate.Title = request.Title;
            serviceToUpdate.Price = request.Price;
            serviceToUpdate.Location = request.Location;
            serviceToUpdate.Duration = request.Duration;
            serviceToUpdate.Description = request.Description;
            serviceToUpdate.Itinerary = request.Itinerary;
            serviceToUpdate.Inclusions = request.Inclusions;
            serviceToUpdate.Exclusions = request.Exclusions;
            serviceToUpdate.Terms = request.Terms;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                status = 200,
                message = "Service updated successfully",
                service = new
                {
                    serviceToUpdate.Id,
                    serviceToUpdate.AgencyId,
                    serviceToUpdate.CategoryId,
                    serviceToUpdate.Title,
                    serviceToUpdate.Price,
                    serviceToUpdate.Location,
                    serviceToUpdate.Duration,
                    serviceToUpdate.Description,
                    serviceToUpdate.Itinerary,
                    serviceToUpdate.Inclusions,
                    serviceToUpdate.Exclusions,
                    serviceToUpdate.Terms
                }
            });
        })
        .WithApiDocumentation("Update service", "Services", "Updates an existing service",
            "Updates a service by its ID, ensuring uniqueness of Agency, Category, and Title.",
            StatusCodes.Status200OK, StatusCodes.Status400BadRequest, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            message = "Service updated successfully",
            service = new
            {
                Id = 1,
                AgencyId = 1,
                CategoryId = 1,
                Title = "Updated Amazing Tour",
                Price = 109.99,
                Location = "Paris, France",
                Duration = "4 days",
                Description = "An even better tour of Paris.",
                Itinerary = "Day 1: Eiffel Tower...",
                Inclusions = "Flights, Hotel, Breakfast",
                Exclusions = "Lunch, Dinner",
                Terms = "Full payment 1 week prior."
            }
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Service not found" })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Agency not found" })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Category not found" })
        .WithResponseExample(StatusCodes.Status409Conflict, new
        {
            status = 409,
            message = "A service with the same Agency, Category, and Title already exists."
        });
        
        app.MapDelete($"/{routePath}/{{id}}", async (int id, DatabaseContext db) =>
        {
            var service = await db.Services.FindAsync(id);
            if (service == null)
                return Results.NotFound(new { status = 404, message = "Service not found" });

            db.Services.Remove(service);
            await db.SaveChangesAsync();

            return Results.Ok(new { status = 200, message = "Service deleted successfully" });
        })
        .WithApiDocumentation("Delete service", "Services", "Deletes a service",
            "Removes a service from the system by its ID.",
            StatusCodes.Status200OK, StatusCodes.Status404NotFound)
        .WithResponseExample(StatusCodes.Status200OK, new { status = 200, message = "Service deleted successfully" })
        .WithResponseExample(StatusCodes.Status404NotFound, new { status = 404, message = "Service not found" });
    }
}