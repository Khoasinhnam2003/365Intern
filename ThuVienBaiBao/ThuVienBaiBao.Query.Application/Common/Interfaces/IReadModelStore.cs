using ThuVienBaiBao.Query.Contracts.Menus;
using ThuVienBaiBao.Query.Contracts.News;

namespace ThuVienBaiBao.Query.Application.Common.Interfaces;

public interface IReadModelStore
{
    Task<IReadOnlyCollection<MenuSummaryDto>> GetMenusAsync(CancellationToken cancellationToken = default);

    Task<MenuDetailDto?> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NewsSummaryDto>> GetNewsAsync(CancellationToken cancellationToken = default);

    Task<NewsDetailDto?> GetNewsByIdAsync(int id, CancellationToken cancellationToken = default);
}