using Microsoft.EntityFrameworkCore;
using WebApplication2.classes.entities;
using WebApplication2.repositories;

namespace WebApplication2.extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DatabaseContext>(options =>
            options.UseSqlite(connectionString));
            
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        
        return services;
    }
}