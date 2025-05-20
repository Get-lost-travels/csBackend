using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;
using WebApplication2.classes.requests.services;
using WebApplication2.extensions;

namespace WebApplication2.routes;

public class ServiceCategories
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        app.MapPost($"/{routePath}/create", async (
            [FromBody] CreateServiceCategoryRequest request,
            DatabaseContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { status = 400, message = "Category name is required" });
            }

            var category = new ServiceCategory
            {
                Name = request.Name,
                Description = request.Description
            };

            db.ServiceCategories.Add(category);

            try
            {
                await db.SaveChangesAsync();

                return Results.Created($"/{routePath}/{category.Id}", new
                {
                    status = 201,
                    message = "Service category created successfully",
                    category
                });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19 && sqliteEx.Message.Contains("UNIQUE constraint failed: service_categories.name"))
                {
                    return Results.Conflict(new { status = 409, message = $"A category with the name '{request.Name}' already exists." });
                }
                else
                {
                    return Results.Json(new { status = 500, message = "An unexpected database error occurred." }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }
        })
        .WithTags("ServiceCategories")
        .WithApiDocumentation("Create category", "ServiceCategories", "Create a service category",
            "Adds a new category that can be used to classify services",
            StatusCodes.Status201Created, StatusCodes.Status400BadRequest, StatusCodes.Status409Conflict)
        .WithResponseExample(StatusCodes.Status201Created, new
        {
            status = 201,
            message = "Service category created successfully",
            category = new
            {
                Id = 1,
                Name = "Tours",
                Description = "Outdoor and city tours"
            }
        })
        .WithResponseExample(StatusCodes.Status400BadRequest, new
        {
            status = 400,
            message = "Category name is required"
        })
        .WithResponseExample(StatusCodes.Status409Conflict, new
        {
            status = 409,
            message = "A category with the name 'Tours' already exists."
        });

        app.MapGet($"/{routePath}", async (DatabaseContext db) =>
        {
            var categories = await db.ServiceCategories.ToListAsync();
            return Results.Ok(new { status = 200, count = categories.Count, categories });
        })
        .WithTags("ServiceCategories")
        .WithApiDocumentation("Get all categories", "ServiceCategories", "Get all service categories",
            "Returns a list of all service categories in the system",
            StatusCodes.Status200OK)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            count = 2,
            categories = new[]
            {
                new { Id = 1, Name = "Tours", Description = "Outdoor and city tours" },
                new { Id = 2, Name = "Transport", Description = "Car rentals and transfers" }
            }
        });

        app.MapGet($"/{routePath}/{{id}}", async (int id, DatabaseContext db) =>
        {
            var category = await db.ServiceCategories.FindAsync(id);
            return category == null
                ? Results.NotFound(new { status = 404, message = "Category not found" })
                : Results.Ok(new { status = 200, category });
        })
        .WithTags("ServiceCategories")
        .WithApiDocumentation("Get category by ID", "ServiceCategories", "Get a service category",
            "Returns a service category by ID",
            StatusCodes.Status200OK, StatusCodes.Status404NotFound)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            category = new { Id = 1, Name = "Tours", Description = "Outdoor and city tours" }
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new
        {
            status = 404,
            message = "Category not found"
        });

        app.MapPut($"/{routePath}/{{id}}", async (int id, [FromBody] CreateServiceCategoryRequest request, DatabaseContext db) =>
        {
            var categoryToUpdate = await db.ServiceCategories.FindAsync(id);
            if (categoryToUpdate == null)
            {
                return Results.NotFound(new { status = 404, message = "Category not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                if (await db.ServiceCategories.AnyAsync(c => c.Name == request.Name && c.Id != id))
                {
                    return Results.Conflict(new { status = 409, message = $"A category with the name '{request.Name}' already exists." });
                }
                categoryToUpdate.Name = request.Name;
            }

            if (request.Description != null)
            {
                categoryToUpdate.Description = request.Description;
            }

            try
            {
                await db.SaveChangesAsync();

                return Results.Ok(new
                {
                    status = 200,
                    message = "Category updated successfully",
                    category = categoryToUpdate
                });
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException is SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19 && sqliteEx.Message.Contains("UNIQUE constraint failed: service_categories.name"))
                {
                    return Results.Conflict(new { status = 409, message = $"A category with the name '{request.Name}' already exists." });
                }
                else
                {
                    return Results.Json(new { status = 500, message = "An unexpected database error occurred." }, statusCode: StatusCodes.Status500InternalServerError);
                }
            }
        })
        .WithTags("ServiceCategories")
        .WithApiDocumentation("Update category", "ServiceCategories", "Update a service category",
            "Updates an existing service category by its ID",
            StatusCodes.Status200OK, StatusCodes.Status404NotFound, StatusCodes.Status409Conflict)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            message = "Category updated successfully",
            category = new { Id = 1, Name = "Updated Tours", Description = "New description for tours" }
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new
        {
            status = 404,
            message = "Category not found"
        })
        .WithResponseExample(StatusCodes.Status409Conflict, new
        {
            status = 409,
            message = "A category with the name 'Existing Category Name' already exists."
        });

        app.MapDelete($"/{routePath}/{{id}}", async (int id, DatabaseContext db) =>
        {
            var category = await db.ServiceCategories.FindAsync(id);
            if (category == null)
                return Results.NotFound(new { status = 404, message = "Category not found" });

            db.ServiceCategories.Remove(category);
            await db.SaveChangesAsync();

            return Results.Ok(new { status = 200, message = "Category deleted successfully" });
        })
        .WithTags("ServiceCategories")
        .WithApiDocumentation("Delete category", "ServiceCategories", "Delete a service category",
            "Removes a service category by its ID",
            StatusCodes.Status200OK, StatusCodes.Status404NotFound)
        .WithResponseExample(StatusCodes.Status200OK, new
        {
            status = 200,
            message = "Category deleted successfully"
        })
        .WithResponseExample(StatusCodes.Status404NotFound, new
        {
            status = 404,
            message = "Category not found"
        });
    }
}