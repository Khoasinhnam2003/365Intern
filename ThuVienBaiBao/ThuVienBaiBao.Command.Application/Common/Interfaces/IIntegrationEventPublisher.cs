using ThuVienBaiBao.Command.Contracts.Integration;

namespace ThuVienBaiBao.Command.Application.Common.Interfaces;

public interface IIntegrationEventPublisher
{
    Task PublishMenuUpsertedAsync(MenuUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task PublishMenuDeletedAsync(MenuDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task PublishNewsUpsertedAsync(NewsUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);

    Task PublishNewsDeletedAsync(NewsDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}