using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;

namespace WebApplication2.middlewares;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, DatabaseContext dbContext)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

        if (!string.IsNullOrEmpty(token))
        {
            var userSession = await dbContext.UserSessions
                .Include(us => us.User)
                .FirstOrDefaultAsync(us => us.Token == token);

            if (userSession != null && userSession.User != null)
            {
                context.Items["User"] = userSession.User;
            }
        }

        await _next(context);
    }
}