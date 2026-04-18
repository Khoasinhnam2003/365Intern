namespace ThuVienBaiBao.Query.Contracts.Integration;

public sealed record RelatedNewsSnapshot(
    int Id,
    string Title,
    string Slug);

public sealed record RelatedMenuSnapshot(
    int Id,
    string Name,
    string Slug);

public sealed record MenuUpsertedIntegrationEvent(
    int Id,
    string Name,
    string Slug,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<RelatedNewsSnapshot> News);

public sealed record MenuDeletedIntegrationEvent(
    int Id,
    IReadOnlyCollection<int> NewsIds);

public sealed record NewsUpsertedIntegrationEvent(
    int Id,
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    DateTime? PublishedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<RelatedMenuSnapshot> Menus);

public sealed record NewsDeletedIntegrationEvent(
    int Id,
    IReadOnlyCollection<int> MenuIds);