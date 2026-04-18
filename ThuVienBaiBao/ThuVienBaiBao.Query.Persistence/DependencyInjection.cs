using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThuVienBaiBao.Query.Application.Common.Interfaces;

namespace ThuVienBaiBao.Query.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton(new MongoDbOptions
        {
            ConnectionString = configuration[$"{MongoDbOptions.SectionName}:ConnectionString"] ?? "mongodb://localhost:27017",
            DatabaseName = configuration[$"{MongoDbOptions.SectionName}:DatabaseName"] ?? "ThuVienBaiBaoReadModel",
            MenusCollectionName = configuration[$"{MongoDbOptions.SectionName}:MenusCollectionName"] ?? "menus",
            NewsCollectionName = configuration[$"{MongoDbOptions.SectionName}:NewsCollectionName"] ?? "news"
        });
        services.AddSingleton(new RabbitMqOptions
        {
            HostName = configuration[$"{RabbitMqOptions.SectionName}:HostName"] ?? "localhost",
            Port = int.TryParse(configuration[$"{RabbitMqOptions.SectionName}:Port"], out var port) ? port : 5672,
            UserName = configuration[$"{RabbitMqOptions.SectionName}:UserName"] ?? "guest",
            Password = configuration[$"{RabbitMqOptions.SectionName}:Password"] ?? "guest",
            VirtualHost = configuration[$"{RabbitMqOptions.SectionName}:VirtualHost"] ?? "/",
            ExchangeName = configuration[$"{RabbitMqOptions.SectionName}:ExchangeName"] ?? "thuvienbaibao.integration",
            QueueName = configuration[$"{RabbitMqOptions.SectionName}:QueueName"] ?? "thuvienbaibao.query.readmodel"
        });
        services.AddSingleton<MongoReadModelStore>();
        services.AddScoped<IReadModelStore>(provider => provider.GetRequiredService<MongoReadModelStore>());

        return services;
    }
}

