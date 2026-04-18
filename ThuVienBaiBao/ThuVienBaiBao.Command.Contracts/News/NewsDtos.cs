namespace ThuVienBaiBao.Command.Contracts.News;

public sealed record NewsSummaryDto(
    int Id,
    string Title,
    string Slug,
    bool IsPublished,
    int MenuCount);

public sealed record NewsRelatedMenuDto(
    int Id,
    string Name,
    string Slug);

public sealed record NewsDetailDto(
    int Id,
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    DateTime? PublishedAt,
    IReadOnlyCollection<NewsRelatedMenuDto> Menus);

public sealed record CreateNewsRequest(
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    IReadOnlyCollection<int>? MenuIds);

public sealed record UpdateNewsRequest(
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    IReadOnlyCollection<int>? MenuIds);

