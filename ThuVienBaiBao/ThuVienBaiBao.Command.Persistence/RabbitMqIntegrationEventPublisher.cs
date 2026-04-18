using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using ThuVienBaiBao.Command.Application.Common.Interfaces;
using ThuVienBaiBao.Command.Contracts.Integration;

namespace ThuVienBaiBao.Command.Persistence;

public sealed class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions options;
    private readonly IConnection connection;
    private readonly IModel channel;

    public RabbitMqIntegrationEventPublisher(RabbitMqOptions options)
    {
        this.options = options;

        var factory = new ConnectionFactory
        {
            HostName = this.options.HostName,
            Port = this.options.Port,
            UserName = this.options.UserName,
            Password = this.options.Password,
            VirtualHost = this.options.VirtualHost,
            DispatchConsumersAsync = true
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        channel.ExchangeDeclare(this.options.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
    }

    public Task PublishMenuUpsertedAsync(MenuUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => PublishAsync("menu.upserted", integrationEvent, cancellationToken);

    public Task PublishMenuDeletedAsync(MenuDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => PublishAsync("menu.deleted", integrationEvent, cancellationToken);

    public Task PublishNewsUpsertedAsync(NewsUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => PublishAsync("news.upserted", integrationEvent, cancellationToken);

    public Task PublishNewsDeletedAsync(NewsDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => PublishAsync("news.deleted", integrationEvent, cancellationToken);

    private Task PublishAsync<TMessage>(string routingKey, TMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2;

        channel.BasicPublish(
            exchange: options.ExchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: payload);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        channel.Dispose();
        connection.Dispose();
    }
}