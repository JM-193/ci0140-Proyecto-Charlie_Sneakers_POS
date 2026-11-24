using CharliesSneakersPOS.Domain.Interfaces;
using CharliesSneakersPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CharliesSneakersPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<EfCashSessionRepository>();
        services.AddScoped<ICashSessionRepository>(sp => sp.GetRequiredService<EfCashSessionRepository>());
        services.AddScoped<ICashSessionStatus>(sp => sp.GetRequiredService<EfCashSessionRepository>());
        services.AddScoped<SaleRegistrationService>();
        services.AddScoped<ICashSalesProvider, EfCashSalesProvider>();
        
        return services;
    }
}
