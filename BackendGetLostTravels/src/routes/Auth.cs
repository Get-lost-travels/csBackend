using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WebApplication2.classes.requests.auth;
using WebApplication2.classes.entities;
using WebApplication2.extensions;

namespace WebApplication2.routes;

public class Auth
{
    public static void RegisterRoutes(WebApplication app, string routePath)
    {
        app.MapPost($"/{routePath}/login",
                async ([FromBody] LoginRequest request,
                    DatabaseContext dbContext) =>
                {
                    if (string.IsNullOrEmpty(request.Email) ||
                        string.IsNullOrEmpty(request.Password))
                    {
                        return Results.BadRequest(new
                        {
                            status = StatusCodes.Status400BadRequest,
                            message = "Email and password are required"
                        });
                    }

                    var user =
                        await dbContext.Users.FirstOrDefaultAsync(u =>
                            u.Email == request.Email);

                    if (user?.PasswordHash == null ||
                        !VerifyPassword(request.Password, user.PasswordHash))
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
                        status = StatusCodes.Status200OK,
                        message = "Login successful",
                        token,
                        user = new
                            { user.Id, user.Username, user.Email, user.Role }
                    });
                }).WithApiDocumentation(
                "Login",
                "Authentication",
                "Authenticates a user",
                "Validates user credentials and returns an authentication token if successful",
                StatusCodes.Status200OK,
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized
            ).WithResponseExample(StatusCodes.Status200OK, new
            {
                status = 200,
                message = "Login successful",
                token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                user = new
                {
                    Id = 1, Username = "john.doe", Email = "john@example.com",
                    Role = "customer"
                }
            })
            .WithResponseExample(StatusCodes.Status400BadRequest, new
            {
                status = 400,
                message = "Email and password are required"
            });

        app.MapPost($"/{routePath}/register",
                async ([FromBody] RegisterRequest request,
                    DatabaseContext dbContext) =>
                {
                    if (string.IsNullOrEmpty(request.Username) ||
                        string.IsNullOrEmpty(request.Email) ||
                        string.IsNullOrEmpty(request.Password))
                    {
                        return Results.BadRequest(new
                        {
                            status = StatusCodes.Status400BadRequest,
                            message =
                                "Username, email, and password are required"
                        });
                    }

                    var existingUser =
                        await dbContext.Users.FirstOrDefaultAsync(u =>
                            u.Email == request.Email);

                    if (existingUser != null)
                    {
                        return Results.BadRequest(new
                        {
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
                        status = StatusCodes.Status201Created,
                        message = "User registered successfully",
                        token,
                        user = new
                            { user.Id, user.Username, user.Email, user.Role }
                    });
                }).WithApiDocumentation(
                "Register",
                "Authentication",
                "Registers a new user",
                "Creates a new user account with provided credentials and returns an authentication token",
                StatusCodes.Status201Created,
                StatusCodes.Status400BadRequest
            )
            .WithResponseExample(StatusCodes.Status201Created, new
            {
                status = 201,
                message = "User registered successfully",
                token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                user = new
                {
                    Id = 1, Username = "jane.doe", Email = "jane@example.com",
                    Role = "customer"
                }
            })
            .WithResponseExample(StatusCodes.Status400BadRequest, new
            {
                status = 400,
                message = "Username, email, and password are required"
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