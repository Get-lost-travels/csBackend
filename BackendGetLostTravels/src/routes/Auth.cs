using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebApplication2.classes.requests.auth;
using WebApplication2.classes.entities;

namespace WebApplication2.routes;

public class Auth
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        app.MapPost($"/{routePath}/login", async ([FromBody] LoginRequest request, DatabaseContext dbContext) =>
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { 
                    status = StatusCodes.Status400BadRequest,
                    message = "Email and password are required" 
                });
            }
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user?.PasswordHash == null  || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var token = GenerateToken();
            var session = new UserSession
            {
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.UserSessions.Add(session);
            await dbContext.SaveChangesAsync();

            return Results.Ok(new
            {
                status  = StatusCodes.Status200OK,
                message = "Login successful",
                token, 
                user = new { user.Id, user.Username, user.Email, user.Role }
            });
        });

        app.MapPost($"/{routePath}/register", async ([FromBody] RegisterRequest request, DatabaseContext dbContext) =>
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return Results.BadRequest(new { 
                    status = StatusCodes.Status400BadRequest,
                    message = "Username, email, and password are required" 
                });
            }
            
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return Results.BadRequest(new { 
                    status = StatusCodes.Status400BadRequest,
                    message = "User with this email already exists" 
                });
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = "customer",
                AuthProvider = "local",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            var token = GenerateToken();
            var session = new UserSession
            {
                UserId = user.Id,
                Token = token,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.UserSessions.Add(session);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/api/users/{user.Id}", new
            {
                status  = StatusCodes.Status201Created,
                message = "User registered successfully",
                token, 
                user = new { user.Id, user.Username, user.Email, user.Role }
            });
        });
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return hash == HashPassword(password);
    }

    private static string GenerateToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
               Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}