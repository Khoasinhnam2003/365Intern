using Microsoft.EntityFrameworkCore;
using DomainMenu = ThuVienBaiBao.Command.Domain.Entities.Menu;
using DomainMenuNews = ThuVienBaiBao.Command.Domain.Entities.MenuNews;
using DomainNews = ThuVienBaiBao.Command.Domain.Entities.News;

namespace ThuVienBaiBao.Command.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<DomainMenu> Menus { get; }

    DbSet<DomainNews> News { get; }

    DbSet<DomainMenuNews> MenuNews { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

