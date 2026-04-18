using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ThuVienBaiBao.Query.Contracts.Integration;

namespace ThuVienBaiBao.Query.Persistence;

public sealed class RabbitMqReadModelConsumerHostedService : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions options;
    private readonly MongoReadModelStore readModelStore;
    private IConnection? connection;
    private IModel? channel;

    public RabbitMqReadModelConsumerHostedService(RabbitMqOptions options, MongoReadModelStore readModelStore)
    {
        this.options = options;
        this.readModelStore = readModelStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            DispatchConsumersAsync = true
        };

        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(options.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.QueueName, options.ExchangeName, "menu.upserted");
        channel.QueueBind(options.QueueName, options.ExchangeName, "menu.deleted");
        channel.QueueBind(options.QueueName, options.ExchangeName, "news.upserted");
        channel.QueueBind(options.QueueName, options.ExchangeName, "news.deleted");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += OnMessageReceivedAsync;

        channel.BasicConsume(options.QueueName, autoAck: false, consumer);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            switch (eventArgs.RoutingKey)
            {
                case "menu.upserted":
                {
                    var integrationEvent = JsonSerializer.Deserialize<MenuUpsertedIntegrationEvent>(eventArgs.Body.Span, SerializerOptions);
                    if (integrationEvent is not null)
                    {
                        await readModelStore.ApplyMenuUpsertedAsync(integrationEvent);
                    }

                    break;
                }
                case "menu.deleted":
                {
                    var integrationEvent = JsonSerializer.Deserialize<MenuDeletedIntegrationEvent>(eventArgs.Body.Span, SerializerOptions);
                    if (integrationEvent is not null)
                    {
                        await readModelStore.ApplyMenuDeletedAsync(integrationEvent);
                    }

                    break;
                }
                case "news.upserted":
                {
                    var integrationEvent = JsonSerializer.Deserialize<NewsUpsertedIntegrationEvent>(eventArgs.Body.Span, SerializerOptions);
                    if (integrationEvent is not null)
                    {
                        await readModelStore.ApplyNewsUpsertedAsync(integrationEvent);
                    }

                    break;
                }
                case "news.deleted":
                {
                    var integrationEvent = JsonSerializer.Deserialize<NewsDeletedIntegrationEvent>(eventArgs.Body.Span, SerializerOptions);
                    if (integrationEvent is not null)
                    {
                        await readModelStore.ApplyNewsDeletedAsync(integrationEvent);
                    }

                    break;
                }
            }

            channel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch
        {
            channel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
        base.Dispose();
    }
}