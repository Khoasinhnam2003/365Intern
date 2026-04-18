using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThuVienBaiBao.Command.Application.Common.Interfaces;

namespace ThuVienBaiBao.Command.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton(new RabbitMqOptions
        {
            HostName = configuration[$"{RabbitMqOptions.SectionName}:HostName"] ?? "localhost",
            Port = int.TryParse(configuration[$"{RabbitMqOptions.SectionName}:Port"], out var port) ? port : 5672,
            UserName = configuration[$"{RabbitMqOptions.SectionName}:UserName"] ?? "guest",
            Password = configuration[$"{RabbitMqOptions.SectionName}:Password"] ?? "guest",
            VirtualHost = configuration[$"{RabbitMqOptions.SectionName}:VirtualHost"] ?? "/",
            ExchangeName = configuration[$"{RabbitMqOptions.SectionName}:ExchangeName"] ?? "thuvienbaibao.integration"
        });
        services.AddSingleton<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

        return services;
    }
}

