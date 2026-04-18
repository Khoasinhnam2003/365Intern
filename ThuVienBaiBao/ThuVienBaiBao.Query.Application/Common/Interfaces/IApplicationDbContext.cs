using Microsoft.EntityFrameworkCore;
using DomainMenu = ThuVienBaiBao.Query.Domain.Entities.Menu;
using DomainMenuNews = ThuVienBaiBao.Query.Domain.Entities.MenuNews;
using DomainNews = ThuVienBaiBao.Query.Domain.Entities.News;

namespace ThuVienBaiBao.Query.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<DomainMenu> Menus { get; }

    DbSet<DomainNews> News { get; }

    DbSet<DomainMenuNews> MenuNews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

