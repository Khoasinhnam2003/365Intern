namespace ThuVienBaiBao.Command.Contracts.Menus;

public sealed record MenuSummaryDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int NewsCount);

public sealed record MenuRelatedNewsDto(
    int Id,
    string Title,
    string Slug);

public sealed record MenuDetailDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<MenuRelatedNewsDto> News);

public sealed record CreateMenuRequest(
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<int>? NewsIds);

public sealed record UpdateMenuRequest(
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<int>? NewsIds);

