using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace WebApplication2.routes;

public class Bookings
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        // CUSTOMER: LIST MY BOOKINGS
        app.MapGet($"/{routePath}/my", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();

            // Use Select to avoid circular reference
            var bookings = await dbContext.Bookings
                .Where(b => b.UserId == user.Id)
                .Select(b => new {
                    b.Id,
                    b.ServiceId,
                    b.UserId,
                    b.BookingDate,
                    b.Status,
                    Service = new
                    {
                        b.Service.Id,
                        b.Service.Title,
                        b.Service.Price,
                        b.Service.Location,
                        b.Service.Duration
                    },
                    ETicket = b.ETicket != null ? new
                    {
                        b.ETicket.Id,
                        b.ETicket.TicketCode,
                        b.ETicket.IssuedAt,
                        b.ETicket.QrCodeUrl
                    } : null
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, bookings });
        });

        // CUSTOMER: CANCEL BOOKING
        app.MapPost($"/{routePath}/{{id}}/cancel", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            var booking = await dbContext.Bookings.Include(b => b.Service).FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);
            if (booking == null) return Results.NotFound();
            if (booking.Status != "pending" && booking.Status != "confirmed") return Results.BadRequest(new { message = "Cannot cancel this booking." });
            booking.Status = "cancelled";
            // Optionally, increment availability
            var availability = await dbContext.ServiceAvailabilities.FirstOrDefaultAsync(a => a.ServiceId == booking.ServiceId && a.StartDate <= booking.BookingDate && a.EndDate >= booking.BookingDate);
            if (availability != null) availability.RemainingSpots++;
            await dbContext.SaveChangesAsync();

            // Return simple object to avoid circular reference
            return Results.Ok(new
            {
                booking.Id,
                booking.Status,
                message = "Booking cancelled successfully"
            });
        });

        // AGENCY: LIST BOOKINGS FOR MY SERVICES
        app.MapGet($"/{routePath}/agency", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "agency") return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (agency == null) return Results.NotFound();

            var bookings = await dbContext.Bookings
                .Where(b => b.Service != null && b.Service.AgencyId == agency.Id)
                .Select(b => new {
                    b.Id,
                    b.ServiceId,
                    b.UserId,
                    b.BookingDate,
                    b.Status,
                    Service = new
                    {
                        b.Service.Id,
                        b.Service.Title,
                        b.Service.Price,
                        b.Service.Location,
                        b.Service.Duration
                    }
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, bookings });
        });

        // AGENCY: CONFIRM BOOKING
        app.MapPost($"/{routePath}/{{id}}/confirm", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "agency") return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (agency == null) return Results.NotFound();
            var booking = await dbContext.Bookings.Include(b => b.Service).FirstOrDefaultAsync(b => b.Id == id && b.Service != null && b.Service.AgencyId == agency.Id);
            if (booking == null) return Results.NotFound();
            if (booking.Status != "pending") return Results.BadRequest(new { message = "Only pending bookings can be confirmed." });
            booking.Status = "confirmed";
            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                booking.Id,
                booking.Status,
                message = "Booking confirmed successfully"
            });
        });

        // AGENCY: MARK BOOKING AS COMPLETED
        app.MapPost($"/{routePath}/{{id}}/complete", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "agency") return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (agency == null) return Results.NotFound();
            var booking = await dbContext.Bookings.Include(b => b.Service).FirstOrDefaultAsync(b => b.Id == id && b.Service != null && b.Service.AgencyId == agency.Id);
            if (booking == null) return Results.NotFound();
            if (booking.Status != "confirmed") return Results.BadRequest(new { message = "Only confirmed bookings can be completed." });
            booking.Status = "completed";
            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                booking.Id,
                booking.Status,
                message = "Booking completed successfully"
            });
        });

        // CUSTOMER: GET BOOKING DETAILS
        app.MapGet($"/{routePath}/{{id}}", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();

            var booking = await dbContext.Bookings
                .Where(b => b.Id == id)
                .Select(b => new {
                    b.Id,
                    b.ServiceId,
                    b.UserId,
                    b.BookingDate,
                    b.Status,
                    Service = new
                    {
                        b.Service.Id,
                        b.Service.Title,
                        b.Service.Price,
                        b.Service.Location,
                        b.Service.Duration,
                        b.Service.AgencyId
                    },
                    ETicket = b.ETicket != null ? new
                    {
                        b.ETicket.Id,
                        b.ETicket.TicketCode,
                        b.ETicket.IssuedAt,
                        b.ETicket.QrCodeUrl
                    } : null
                })
                .FirstOrDefaultAsync();

            if (booking == null) return Results.NotFound();

            // Only allow the customer who booked, the agency owner, or webadmin
            if (user.Role == "customer" && booking.UserId != user.Id) return Results.Forbid();
            if (user.Role == "agency")
            {
                var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (agency == null || booking.Service?.AgencyId != agency.Id) return Results.Forbid();
            }
            // webadmin can view all
            return Results.Ok(booking);
        });

        // CUSTOMER: GET E-TICKET
        app.MapGet($"/{routePath}/{{id}}/eticket", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            var booking = await dbContext.Bookings.Include(b => b.ETicket).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null || booking.ETicket == null) return Results.NotFound();
            if (user.Role == "customer" && booking.UserId != user.Id) return Results.Forbid();
            if (user.Role == "agency")
            {
                var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (agency == null || booking.Service == null || booking.Service.AgencyId != agency.Id) return Results.Forbid();
            }

            // Return only ETicket data without circular reference
            return Results.Ok(new
            {
                booking.ETicket.Id,
                booking.ETicket.TicketCode,
                booking.ETicket.IssuedAt,
                booking.ETicket.QrCodeUrl
            });
        });

        // CUSTOMER: REQUEST REFUND
        app.MapPost($"/{routePath}/{{id}}/refund", async (int id, [FromBody] string reason, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "customer") return Results.Forbid();
            var booking = await dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);
            if (booking == null) return Results.NotFound();
            if (booking.Status != "completed" && booking.Status != "confirmed") return Results.BadRequest(new { message = "Refunds can only be requested for confirmed or completed bookings." });
            var dispute = new RefundDispute
            {
                BookingId = booking.Id,
                OpenedBy = user.Id,
                Status = "open",
                Reason = reason,
                OpenedAt = DateTime.UtcNow
            };
            dbContext.RefundDisputes.Add(dispute);
            await dbContext.SaveChangesAsync();
            return Results.Created($"/{routePath}/{id}/refund/{dispute.Id}", new
            {
                dispute.Id,
                dispute.BookingId,
                dispute.Status,
                dispute.Reason,
                dispute.OpenedAt
            });
        });

        // AGENCY: RESPOND TO REFUND
        app.MapPost($"/{routePath}/refunds/{{disputeId}}/respond", async (int disputeId, [FromBody] string response, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "agency") return Results.Forbid();
            var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
            if (agency == null) return Results.NotFound();
            var dispute = await dbContext.RefundDisputes.Include(d => d.Booking).ThenInclude(b => b.Service).FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null || dispute.Booking == null || dispute.Booking.Service == null || dispute.Booking.Service.AgencyId != agency.Id) return Results.Forbid();
            dispute.AgencyResponse = response;
            dispute.Status = "agency_responded";
            await dbContext.SaveChangesAsync();
            return Results.Ok(new
            {
                dispute.Id,
                dispute.Status,
                dispute.AgencyResponse
            });
        });

        // WEBADMIN: LIST ALL BOOKINGS
        app.MapGet($"/{routePath}/all", async (DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "webadmin") return Results.Forbid();
            var bookings = await dbContext.Bookings
                .Select(b => new {
                    b.Id,
                    b.ServiceId,
                    b.UserId,
                    b.BookingDate,
                    b.Status,
                    Service = new
                    {
                        b.Service.Id,
                        b.Service.Title,
                        b.Service.Price,
                        b.Service.Location
                    },
                    User = new
                    {
                        b.User.Id,
                        b.User.Username,
                        b.User.Email
                    }
                })
                .ToListAsync();
            return Results.Ok(new { status = StatusCodes.Status200OK, bookings });
        });

        // WEBADMIN: HANDLE REFUND DISPUTE
        app.MapPost($"/{routePath}/refunds/{{disputeId}}/verdict", async (int disputeId, [FromBody] string verdict, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null || user.Role != "webadmin") return Results.Forbid();
            var dispute = await dbContext.RefundDisputes.FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute == null) return Results.NotFound();
            dispute.AdminVerdict = verdict;
            dispute.Status = "resolved";
            dispute.ResolvedBy = user.Id;
            dispute.ResolvedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
            return Results.Ok(new
            {
                dispute.Id,
                dispute.Status,
                dispute.AdminVerdict
            });
        });

        // CUSTOMER/AGENCY/ADMIN: DOWNLOAD E-TICKET PDF (real PDF)
        app.MapGet($"/{routePath}/{{id}}/eticket/download", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            var booking = await dbContext.Bookings.Include(b => b.ETicket).Include(b => b.Service).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null || booking.ETicket == null) return Results.NotFound();
            if (user.Role == "customer" && booking.UserId != user.Id) return Results.Forbid();
            if (user.Role == "agency")
            {
                var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (agency == null || booking.Service == null || booking.Service.AgencyId != agency.Id) return Results.Forbid();
            }
            // Generate PDF using QuestPDF
            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content()
                        .Column(col =>
                        {
                            col.Item().Text($"E-Ticket for Booking #{booking.Id}").FontSize(20).Bold();
                            col.Item().Text($"Service: {booking.Service?.Title}");
                            col.Item().Text($"Ticket Code: {booking.ETicket.TicketCode}");
                            col.Item().Text($"Issued: {booking.ETicket.IssuedAt}");
                        });
                });
            }).GeneratePdf();
            return Results.File(pdfBytes, "application/pdf", $"eticket_{booking.Id}.pdf");
        });

        // CUSTOMER/AGENCY/ADMIN: DOWNLOAD INVOICE PDF (real PDF)
        app.MapGet($"/{routePath}/{{id}}/invoice/download", async (int id, DatabaseContext dbContext, HttpContext httpContext) =>
        {
            var user = httpContext.Items["User"] as User;
            if (user == null) return Results.Forbid();
            var booking = await dbContext.Bookings.Include(b => b.Service).Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return Results.NotFound();
            if (user.Role == "customer" && booking.UserId != user.Id) return Results.Forbid();
            if (user.Role == "agency")
            {
                var agency = await dbContext.Agencies.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (agency == null || booking.Service == null || booking.Service.AgencyId != agency.Id) return Results.Forbid();
            }
            // Generate PDF using QuestPDF
            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content()
                        .Column(col =>
                        {
                            col.Item().Text($"Invoice for Booking #{booking.Id}").FontSize(20).Bold();
                            col.Item().Text($"Customer: {booking.User?.Username}");
                            col.Item().Text($"Service: {booking.Service?.Title}");
                            col.Item().Text($"Date: {booking.BookingDate}");
                            col.Item().Text($"Status: {booking.Status}");
                        });
                });
            }).GeneratePdf();
            return Results.File(pdfBytes, "application/pdf", $"invoice_{booking.Id}.pdf");
        });
    }
}
