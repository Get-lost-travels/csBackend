using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;

namespace WebApplication2.routes;

public class Services
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        // GET /services?minPrice=...&maxPrice=...&location=...&duration=...&agencyId=...&categoryId=...&minRating=...&page=...&pageSize=...
        app.MapGet($"/{routePath}", async (
            [FromQuery] double? minPrice,
            [FromQuery] double? maxPrice,
            [FromQuery] string? location,
            [FromQuery] int? duration,
            [FromQuery] int? agencyId,
            [FromQuery] int? categoryId,
            [FromQuery] double? minRating,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDir,
            DatabaseContext dbContext,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var query = dbContext.Services
                .Include(s => s.Agency)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(s => s.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(s => s.Price <= maxPrice.Value);
            if (!string.IsNullOrEmpty(location))
                query = query.Where(s => s.Location.ToLower().Contains(location.ToLower()));
            if (duration.HasValue)
                query = query.Where(s => s.Duration == duration.Value);
            if (agencyId.HasValue)
                query = query.Where(s => s.AgencyId == agencyId.Value);
            if (categoryId.HasValue)
                query = query.Where(s => s.CategoryId == categoryId.Value);
            if (minRating.HasValue)
                query = query.Where(s => s.Reviews.Any() && s.Reviews.Average(r => r.Rating) >= minRating.Value);

            if (!string.IsNullOrEmpty(sortBy))
            {
                bool desc = sortDir == "desc";
                switch (sortBy.ToLower())
                {
                    case "price": query = desc ? query.OrderByDescending(s => s.Price) : query.OrderBy(s => s.Price); break;
                    case "rating": query = desc ? query.OrderByDescending(s => s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : 0) : query.OrderBy(s => s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : 0); break;
                    case "duration": query = desc ? query.OrderByDescending(s => s.Duration) : query.OrderBy(s => s.Duration); break;
                    default: query = query.OrderBy(s => s.Title); break;
                }
            }

            var total = await query.CountAsync();
            var services = await query
                .OrderBy(s => s.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new {
                    s.Id,
                    s.Title,
                    s.Price,
                    s.Location,
                    s.Duration,
                    s.Description,
                    Agency = s.Agency != null ? new { s.Agency.Id, s.Agency.Name, s.Agency.LogoUrl } : null,
                    Category = s.Category != null ? new { s.Category.Id, s.Category.Name } : null,
                    AverageRating = s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : null,
                    ReviewCount = s.Reviews.Count,
                    FeaturedMedia = s.ServiceMedia.OrderByDescending(m => m.IsFeatured).ThenBy(m => m.Id).Select(m => new { m.Id, m.MediaType, m.Url, m.Caption, m.IsFeatured }).FirstOrDefault()
                })
                .ToListAsync();

            return Results.Ok(new {
                status = StatusCodes.Status200OK,
                total,
                page,
                pageSize,
                services
            });
        });

        // CREATE a new service (Agency or WebAdmin only)
        app.MapPost($"/{routePath}", async ([FromBody] Service service, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            // Example: get user role from claims (adjust as needed)
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            dbContext.Services.Add(service);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/{service.Id}", service);
        });

        // UPDATE a service (Agency or WebAdmin only)
        app.MapPut($"/{routePath}/{{id}}", async (int id, [FromBody] Service updated, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var service = await dbContext.Services.Include(s => s.Agency).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();

            // Only allow agency owner or webadmin
            if (role == "agency")
            {
                if (service.Agency == null) return Results.Forbid();
                var agency = service.Agency != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == service.AgencyId)
                    : null;
                if (service.Agency == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            // Update fields (add more as needed)
            service.Title = updated.Title;
            service.Price = updated.Price;
            service.Location = updated.Location;
            service.Duration = updated.Duration;
            service.Description = updated.Description;
            service.CategoryId = updated.CategoryId;
            service.AgencyId = updated.AgencyId;
            // ... add more fields as needed ...
            await dbContext.SaveChangesAsync();
            return Results.Ok(service);
        });

        // DELETE a service (Agency or WebAdmin only)
        app.MapDelete($"/{routePath}/{{id}}", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var service = await dbContext.Services.Include(s => s.Agency).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();

            // Only allow agency owner or webadmin
            if (role == "agency")
            {
                if (service.Agency == null) return Results.Forbid();
                var agency = service.Agency != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == service.AgencyId)
                    : null;
                if (service.Agency == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            dbContext.Services.Remove(service);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // GET service details by id (any user)
        app.MapGet($"/{routePath}/{{id}}", async (int id, DatabaseContext dbContext) =>
        {
            var service = await dbContext.Services
                .Include(s => s.Agency)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.ServiceMedia)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();
            return Results.Ok(new {
                service.Id,
                service.Title,
                service.Price,
                service.Location,
                service.Duration,
                service.Description,
                service.Itinerary,
                service.Inclusions,
                service.Exclusions,
                service.Terms,
                Agency = service.Agency != null ? new { service.Agency.Id, service.Agency.Name, service.Agency.LogoUrl } : null,
                Category = service.Category != null ? new { service.Category.Id, service.Category.Name } : null,
                AverageRating = service.Reviews.Any() ? service.Reviews.Average(r => r.Rating) : null,
                ReviewCount = service.Reviews.Count,
                Media = service.ServiceMedia.Select(m => new { m.Id, m.MediaType, m.Url, m.Caption, m.IsFeatured })
            });
        });

        // GET all service categories (any user)
        app.MapGet($"/{routePath}/categories", async (DatabaseContext dbContext) =>
        {
            var categories = await dbContext.ServiceCategories
                .Select(c => new { c.Id, c.Name, c.Description })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, categories });
        });

        // CATEGORY MANAGEMENT (WebAdmin only)
        // CREATE category
        app.MapPost($"/{routePath}/categories", async ([FromBody] ServiceCategory category, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role != "webadmin") return Results.Forbid();
            dbContext.ServiceCategories.Add(category);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/categories/{category.Id}", category);
        });
        // UPDATE category
        app.MapPut($"/{routePath}/categories/{{categoryId}}", async (int categoryId, [FromBody] ServiceCategory updated, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role != "webadmin") return Results.Forbid();
            var category = await dbContext.ServiceCategories.FindAsync(categoryId);
            if (category == null) return Results.NotFound();
            category.Name = updated.Name;
            category.Description = updated.Description;
            await dbContext.SaveChangesAsync();
            return Results.Ok(category);
        });
        // DELETE category
        app.MapDelete($"/{routePath}/categories/{{categoryId}}", async (int categoryId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            if (role != "webadmin") return Results.Forbid();
            var category = await dbContext.ServiceCategories.FindAsync(categoryId);
            if (category == null) return Results.NotFound();
            dbContext.ServiceCategories.Remove(category);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // AGENCY: LIST MY SERVICES
        app.MapGet($"/{routePath}/my", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency") return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == userId);
            if (agency == null) return Results.NotFound();
            var services = await dbContext.Services
                .Where(s => s.AgencyId == agency.Id)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.ServiceMedia)
                .Select(s => new {
                    s.Id,
                    s.Title,
                    s.Price,
                    s.Location,
                    s.Duration,
                    s.Description,
                    Category = s.Category != null ? new { s.Category.Id, s.Category.Name } : null,
                    AverageRating = s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : null,
                    ReviewCount = s.Reviews.Count,
                    FeaturedMedia = s.ServiceMedia.OrderByDescending(m => m.IsFeatured).ThenBy(m => m.Id).Select(m => new { m.Id, m.MediaType, m.Url, m.Caption, m.IsFeatured }).FirstOrDefault()
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, services });
        });

        // UPLOAD media for a service (Agency or WebAdmin only)
        app.MapPost($"/{routePath}/{{id}}/media", async (int id, HttpRequest request, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var service = await dbContext.Services.Include(s => s.Agency).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();
            if (role == "agency")
            {
                if (service.Agency == null) return Results.Forbid();
                var agency = service.Agency != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == service.AgencyId)
                    : null;
                if (service.Agency == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            if (!request.HasFormContentType)
                return Results.BadRequest(new { message = "Content-Type must be multipart/form-data" });
            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { message = "No file uploaded" });
            var mediaType = form["mediaType"].ToString();
            var caption = form["caption"].ToString();
            var isFeatured = form["isFeatured"].ToString() == "true";

            // Save file to disk (for demo, use wwwroot/uploads/)
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"service_{id}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploads, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            var url = $"/uploads/{fileName}";

            var media = new ServiceMedium
            {
                ServiceId = id,
                MediaType = string.IsNullOrEmpty(mediaType) ? "image" : mediaType,
                Url = url,
                Caption = caption,
                IsFeatured = isFeatured,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.ServiceMedia.Add(media);
            await dbContext.SaveChangesAsync();
            return Results.Created(url, new { media.Id, media.MediaType, media.Url, media.Caption, media.IsFeatured });
        });

        // DELETE media for a service (Agency or WebAdmin only)
        app.MapDelete($"/{routePath}/{{id}}/media/{{mediaId}}", async (int id, int mediaId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var media = await dbContext.ServiceMedia.Include(m => m.Service).ThenInclude(s => s.Agency).FirstOrDefaultAsync(m => m.Id == mediaId && m.ServiceId == id);
            if (media == null) return Results.NotFound();
            if (role == "agency")
            {
                if (media.Service == null) return Results.Forbid();
                var agency = media.Service != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == media.Service.AgencyId)
                    : null;
                if (media.Service == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }
            dbContext.ServiceMedia.Remove(media);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // UPDATE media for a service (Agency or WebAdmin only)
        app.MapPut($"/{routePath}/{{id}}/media/{{mediaId}}", async (int id, int mediaId, [FromBody] ServiceMedium updated, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var media = await dbContext.ServiceMedia.Include(m => m.Service).ThenInclude(s => s.Agency).FirstOrDefaultAsync(m => m.Id == mediaId && m.ServiceId == id);
            if (media == null) return Results.NotFound();
            if (role == "agency")
            {
                if (media.Service == null) return Results.Forbid();
                var agency = media.Service != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == media.Service.AgencyId)
                    : null;
                if (media.Service == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }
            media.Caption = updated.Caption;
            media.IsFeatured = updated.IsFeatured;
            media.MediaType = updated.MediaType;
            await dbContext.SaveChangesAsync();
            return Results.Ok(media);
        });

        // SET a media as featured (Agency or WebAdmin only)
        app.MapPost($"/{routePath}/{{id}}/media/{{mediaId}}/feature", async (int id, int mediaId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var media = await dbContext.ServiceMedia.Include(m => m.Service).ThenInclude(s => s.Agency).FirstOrDefaultAsync(m => m.Id == mediaId && m.ServiceId == id);
            if (media == null) return Results.NotFound();
            if (role == "agency")
            {
                if (media.Service == null) return Results.Forbid();
                var agency = media.Service != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == media.Service.AgencyId)
                    : null;
                if (media.Service == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }
            // Unset all other featured media for this service
            var allMedia = dbContext.ServiceMedia.Where(m => m.ServiceId == id);
            await allMedia.ForEachAsync(m => m.IsFeatured = false);
            media.IsFeatured = true;
            await dbContext.SaveChangesAsync();
            return Results.Ok(media);
        });

        // GET availability for a service (any user)
        app.MapGet($"/{routePath}/{{id}}/availability", async (int id, DatabaseContext dbContext) =>
        {
            var availability = await dbContext.ServiceAvailabilities
                .Where(a => a.ServiceId == id)
                .Select(a => new {
                    a.Id,
                    a.StartDate,
                    a.EndDate,
                    a.Capacity,
                    a.RemainingSpots
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, availability });
        });

        // CREATE availability for a service (Agency or WebAdmin only)
        app.MapPost($"/{routePath}/{{id}}/availability", async (int id, [FromBody] ServiceAvailability availability, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var service = await dbContext.Services.Include(s => s.Agency).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();
            if (role == "agency")
            {
                if (service.Agency == null) return Results.Forbid();
                var agency = service.Agency != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == service.AgencyId)
                    : null;
                if (service.Agency == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            availability.ServiceId = id;
            dbContext.ServiceAvailabilities.Add(availability);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/{id}/availability/{availability.Id}", availability);
        });

        // UPDATE availability for a service (Agency or WebAdmin only)
        app.MapPut($"/{routePath}/{{id}}/availability/{{availabilityId}}", async (int id, int availabilityId, [FromBody] ServiceAvailability updated, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var availability = await dbContext.ServiceAvailabilities.Include(a => a.Service).ThenInclude(s => s.Agency).FirstOrDefaultAsync(a => a.Id == availabilityId && a.ServiceId == id);
            if (availability == null) return Results.NotFound();
            if (role == "agency")
            {
                if (availability.Service == null) return Results.Forbid();
                var agency = availability.Service != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == availability.Service.AgencyId)
                    : null;
                if (availability.Service == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            availability.StartDate = updated.StartDate;
            availability.EndDate = updated.EndDate;
            availability.Capacity = updated.Capacity;
            availability.RemainingSpots = updated.RemainingSpots;
            await dbContext.SaveChangesAsync();
            return Results.Ok(availability);
        });

        // DELETE availability for a service (Agency or WebAdmin only)
        app.MapDelete($"/{routePath}/{{id}}/availability/{{availabilityId}}", async (int id, int availabilityId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var role = httpContext.User.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
            var userIdStr = httpContext.User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            int.TryParse(userIdStr, out var userId);
            if (role != "agency" && role != "webadmin")
                return Results.Forbid();

            var availability = await dbContext.ServiceAvailabilities.Include(a => a.Service).ThenInclude(s => s.Agency).FirstOrDefaultAsync(a => a.Id == availabilityId && a.ServiceId == id);
            if (availability == null) return Results.NotFound();
            if (role == "agency")
            {
                if (availability.Service == null) return Results.Forbid();
                var agency = availability.Service != null
                    ? await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == availability.Service.AgencyId)
                    : null;
                if (availability.Service == null || agency == null || agency.UserId != userId)
                    return Results.Forbid();
            }

            dbContext.ServiceAvailabilities.Remove(availability);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // SAVE SEARCHES (Customer)
        // SAVE a search
        app.MapPost($"/{routePath}/saved-searches", async ([FromBody] UserSavedSearch search, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            search.UserId = user.Id;
            search.CreatedAt = DateTime.UtcNow;
            dbContext.UserSavedSearches.Add(search);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/saved-searches/{search.Request}", search);
        });
        // LIST saved searches
        app.MapGet($"/{routePath}/saved-searches", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            var searches = await dbContext.UserSavedSearches.Where(s => s.UserId == user.Id).ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, searches });
        });
        // DELETE saved search
        app.MapDelete($"/{routePath}/saved-searches/{{searchId}}", async (int searchId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            var search = await dbContext.UserSavedSearches.FirstOrDefaultAsync(s => s.Request == searchId && s.UserId == user.Id);
            if (search == null) return Results.NotFound();
            dbContext.UserSavedSearches.Remove(search);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // REVIEWS (no status, webadmin can delete any review)
        // CREATE review (Customer only, must have completed booking)
        app.MapPost($"/{routePath}/{{id}}/reviews", async (int id, [FromBody] Review review, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            // Check if user has a completed booking for this service
            var hasCompletedBooking = await dbContext.Bookings.AnyAsync(b => b.ServiceId == id && b.UserId == user.Id && b.Status == "completed");
            if (!hasCompletedBooking) return Results.BadRequest(new { message = "You can only review services you have completed." });
            // Only one review per user per service
            var alreadyReviewed = await dbContext.Reviews.AnyAsync(r => r.ServiceId == id && r.UserId == user.Id);
            if (alreadyReviewed) return Results.BadRequest(new { message = "You have already reviewed this service." });
            review.ServiceId = id;
            review.UserId = user.Id;
            review.CreatedAt = DateTime.UtcNow;
            dbContext.Reviews.Add(review);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/{id}/reviews/{review.Id}", review);
        });

        // UPDATE review (Customer only, only own review)
        app.MapPut($"/{routePath}/{{id}}/reviews/{{reviewId}}", async (int id, int reviewId, [FromBody] Review updated, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ServiceId == id && r.UserId == user.Id);
            if (review == null) return Results.NotFound();
            review.Rating = updated.Rating;
            review.Comment = updated.Comment;
            review.CreatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return Results.Ok(review);
        });

        // DELETE review (Customer: only own, Webadmin: any)
        app.MapDelete($"/{routePath}/{{id}}/reviews/{{reviewId}}", async (int id, int reviewId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            var review = await dbContext.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.ServiceId == id);
            if (review == null) return Results.NotFound();
            if (user == null || (user.Role != "webadmin" && review.UserId != user.Id)) return Results.Forbid();
            dbContext.Reviews.Remove(review);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // LIST reviews for a service (any user, all reviews)
        app.MapGet($"/{routePath}/{{id}}/reviews", async (int id, DatabaseContext dbContext) =>
        {
            var reviews = await dbContext.Reviews
                .Where(r => r.ServiceId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    User = new { r.UserId, Username = r.User.Username }
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, reviews });
        });

        // Switch user role between 'customer' and 'agency' (if user owns an agency)
        app.MapPost($"/{routePath}/switch-role", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            // Check if user owns an agency
            var ownsAgency = await dbContext.Agencies.AnyAsync(a => a.UserId == user.Id);
            if (!ownsAgency) return Results.BadRequest(new { message = "You must own an agency to switch to agency role." });
            // Toggle role
            if (user.Role == "agency")
                user.Role = "customer";
            else if (user.Role == "customer")
                user.Role = "agency";
            else
                return Results.BadRequest(new { message = "Role switching only allowed between customer and agency." });
            await dbContext.SaveChangesAsync();
            return Results.Ok(new { user.Id, user.Username, user.Role });
        });

        // On agency creation, set owner role to 'agency' if not already
        app.MapPost($"/{routePath}/agencies", async ([FromBody] Agency agency, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            agency.UserId = user.Id;
            dbContext.Agencies.Add(agency);
            if (user.Role != "agency")
            {
                user.Role = "agency";
            }
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/agencies/{agency.Id}", agency);
        });

        // DELETE agency (owner or webadmin only)
        app.MapDelete($"/{routePath}/agencies/{{agencyId}}", async (int agencyId, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.Id == agencyId);
            if (agency == null) return Results.NotFound();
            var isOwner = agency.UserId == user.Id;
            var isAdmin = user.Role == "webadmin";
            if (!isOwner && !isAdmin) return Results.Forbid();
            dbContext.Agencies.Remove(agency);
            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        // CUSTOMER: BOOK A SERVICE
        app.MapPost($"/{routePath}/{{id}}/book", async (int id, [FromBody] Booking bookingRequest, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();

            var service = await dbContext.Services.Include(s => s.ServiceAvailabilities).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return Results.NotFound();
            var availability = service.ServiceAvailabilities.FirstOrDefault(a => a.RemainingSpots > 0 && a.StartDate <= DateTime.UtcNow && a.EndDate >= DateTime.UtcNow);
            if (availability == null) return Results.BadRequest(new { message = "No available slots for this service." });

            availability.RemainingSpots--;

            var booking = new Booking
            {
                UserId = user.Id,
                ServiceId = id,
                BookingDate = DateTime.UtcNow,
                Status = "pending",
            };
            dbContext.Bookings.Add(booking);
            await dbContext.SaveChangesAsync();

            var ticketCode = $"TKT-{booking.Id}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            var eticket = new ETicket
            {
                BookingId = booking.Id,
                TicketCode = ticketCode,
                IssuedAt = DateTime.UtcNow,
            };
            dbContext.ETickets.Add(eticket);
            await dbContext.SaveChangesAsync();
            booking.ETicket = eticket;
            return Results.Created($"/{routePath}/{booking.Id}", booking);
        });
    }
}
