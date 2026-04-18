using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using ThuVienBaiBao.Query.Application.Common.Interfaces;
using ThuVienBaiBao.Query.Contracts.Integration;
using ThuVienBaiBao.Query.Contracts.Menus;
using ThuVienBaiBao.Query.Contracts.News;

namespace ThuVienBaiBao.Query.Persistence;

public sealed class MongoReadModelStore : IReadModelStore
{
    private readonly IMongoCollection<MenuReadModelDocument> menusCollection;
    private readonly IMongoCollection<NewsReadModelDocument> newsCollection;

    public MongoReadModelStore(MongoDbOptions options)
    {
        var client = new MongoClient(options.ConnectionString);
        var database = client.GetDatabase(options.DatabaseName);

        menusCollection = database.GetCollection<MenuReadModelDocument>(options.MenusCollectionName);
        newsCollection = database.GetCollection<NewsReadModelDocument>(options.NewsCollectionName);
    }

    public async Task<IReadOnlyCollection<MenuSummaryDto>> GetMenusAsync(CancellationToken cancellationToken = default)
    {
        var menus = await menusCollection.Find(FilterDefinition<MenuReadModelDocument>.Empty)
            .SortBy(menu => menu.Name)
            .ToListAsync(cancellationToken);

        return menus
            .Select(menu => new MenuSummaryDto(menu.Id, menu.Name, menu.Slug, menu.Description, menu.News.Count))
            .ToList();
    }

    public async Task<MenuDetailDto?> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var menu = await menusCollection.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);

        if (menu is null)
        {
            return null;
        }

        return new MenuDetailDto(
            menu.Id,
            menu.Name,
            menu.Slug,
            menu.Description,
            menu.News
                .OrderBy(item => item.Title)
                .Select(item => new MenuRelatedNewsDto(item.Id, item.Title, item.Slug))
                .ToList());
    }

    public async Task<IReadOnlyCollection<NewsSummaryDto>> GetNewsAsync(CancellationToken cancellationToken = default)
    {
        var news = await newsCollection.Find(FilterDefinition<NewsReadModelDocument>.Empty)
            .SortByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return news
            .Select(item => new NewsSummaryDto(item.Id, item.Title, item.Slug, item.IsPublished, item.Menus.Count))
            .ToList();
    }

    public async Task<NewsDetailDto?> GetNewsByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var news = await newsCollection.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);

        if (news is null)
        {
            return null;
        }

        return new NewsDetailDto(
            news.Id,
            news.Title,
            news.Slug,
            news.Content,
            news.IsPublished,
            news.PublishedAt,
            news.Menus
                .OrderBy(item => item.Name)
                .Select(item => new NewsRelatedMenuDto(item.Id, item.Name, item.Slug))
                .ToList());
    }

    public async Task SeedFromSqlAsync(ApplicationDbContext sqlContext, CancellationToken cancellationToken = default)
    {
        var menus = await sqlContext.Menus
            .AsNoTracking()
            .Include(menu => menu.MenuNews)
            .ThenInclude(link => link.News)
            .ToListAsync(cancellationToken);

        foreach (var menu in menus)
        {
            await menusCollection.ReplaceOneAsync(
                item => item.Id == menu.Id,
                new MenuReadModelDocument
                {
                    Id = menu.Id,
                    Name = menu.Name,
                    Slug = menu.Slug,
                    Description = menu.Description,
                    CreatedAt = menu.CreatedAt,
                    UpdatedAt = menu.UpdatedAt,
                    News = menu.MenuNews
                        .OrderBy(link => link.News.Title)
                        .Select(link => new RelatedNewsSnapshot(link.News.Id, link.News.Title, link.News.Slug))
                        .ToList()
                },
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }

        var news = await sqlContext.News
            .AsNoTracking()
            .Include(item => item.MenuNews)
            .ThenInclude(link => link.Menu)
            .ToListAsync(cancellationToken);

        foreach (var item in news)
        {
            await newsCollection.ReplaceOneAsync(
                x => x.Id == item.Id,
                new NewsReadModelDocument
                {
                    Id = item.Id,
                    Title = item.Title,
                    Slug = item.Slug,
                    Content = item.Content,
                    IsPublished = item.IsPublished,
                    PublishedAt = item.PublishedAt,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    Menus = item.MenuNews
                        .OrderBy(link => link.Menu.Name)
                        .Select(link => new RelatedMenuSnapshot(link.Menu.Id, link.Menu.Name, link.Menu.Slug))
                        .ToList()
                },
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }

    public async Task ApplyMenuUpsertedAsync(MenuUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var menuDocument = new MenuReadModelDocument
        {
            Id = integrationEvent.Id,
            Name = integrationEvent.Name,
            Slug = integrationEvent.Slug,
            Description = integrationEvent.Description,
            CreatedAt = integrationEvent.CreatedAt,
            UpdatedAt = integrationEvent.UpdatedAt,
            News = integrationEvent.News.ToList()
        };

        await menusCollection.ReplaceOneAsync(
            item => item.Id == menuDocument.Id,
            menuDocument,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        foreach (var relatedNews in integrationEvent.News)
        {
            await UpdateNewsMenuReferenceAsync(menuDocument, relatedNews, cancellationToken);
        }
    }

    public async Task ApplyMenuDeletedAsync(MenuDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await menusCollection.DeleteOneAsync(item => item.Id == integrationEvent.Id, cancellationToken);

        foreach (var relatedNewsId in integrationEvent.NewsIds)
        {
            var news = await newsCollection.Find(item => item.Id == relatedNewsId).FirstOrDefaultAsync(cancellationToken);

            if (news is null)
            {
                continue;
            }

            news.Menus.RemoveAll(item => item.Id == integrationEvent.Id);

            await newsCollection.ReplaceOneAsync(
                item => item.Id == news.Id,
                news,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }

    public async Task ApplyNewsUpsertedAsync(NewsUpsertedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var newsDocument = new NewsReadModelDocument
        {
            Id = integrationEvent.Id,
            Title = integrationEvent.Title,
            Slug = integrationEvent.Slug,
            Content = integrationEvent.Content,
            IsPublished = integrationEvent.IsPublished,
            PublishedAt = integrationEvent.PublishedAt,
            CreatedAt = integrationEvent.CreatedAt,
            UpdatedAt = integrationEvent.UpdatedAt,
            Menus = integrationEvent.Menus.ToList()
        };

        await newsCollection.ReplaceOneAsync(
            item => item.Id == newsDocument.Id,
            newsDocument,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        foreach (var relatedMenu in integrationEvent.Menus)
        {
            await UpdateMenuNewsReferenceAsync(newsDocument, relatedMenu, cancellationToken);
        }
    }

    public async Task ApplyNewsDeletedAsync(NewsDeletedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await newsCollection.DeleteOneAsync(item => item.Id == integrationEvent.Id, cancellationToken);

        foreach (var relatedMenuId in integrationEvent.MenuIds)
        {
            var menu = await menusCollection.Find(item => item.Id == relatedMenuId).FirstOrDefaultAsync(cancellationToken);

            if (menu is null)
            {
                continue;
            }

            menu.News.RemoveAll(item => item.Id == integrationEvent.Id);

            await menusCollection.ReplaceOneAsync(
                item => item.Id == menu.Id,
                menu,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }

    private async Task UpdateNewsMenuReferenceAsync(MenuReadModelDocument menuDocument, RelatedNewsSnapshot relatedNews, CancellationToken cancellationToken)
    {
        var news = await newsCollection.Find(item => item.Id == relatedNews.Id).FirstOrDefaultAsync(cancellationToken);

        if (news is null)
        {
            return;
        }

        news.Menus.RemoveAll(item => item.Id == menuDocument.Id);
        news.Menus.Add(new RelatedMenuSnapshot(menuDocument.Id, menuDocument.Name, menuDocument.Slug));
        news.Menus = news.Menus.OrderBy(item => item.Name).ToList();

        await newsCollection.ReplaceOneAsync(
            item => item.Id == news.Id,
            news,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private async Task UpdateMenuNewsReferenceAsync(NewsReadModelDocument newsDocument, RelatedMenuSnapshot relatedMenu, CancellationToken cancellationToken)
    {
        var menu = await menusCollection.Find(item => item.Id == relatedMenu.Id).FirstOrDefaultAsync(cancellationToken);

        if (menu is null)
        {
            return;
        }

        menu.News.RemoveAll(item => item.Id == newsDocument.Id);
        menu.News.Add(new RelatedNewsSnapshot(newsDocument.Id, newsDocument.Title, newsDocument.Slug));
        menu.News = menu.News.OrderBy(item => item.Title).ToList();

        await menusCollection.ReplaceOneAsync(
            item => item.Id == menu.Id,
            menu,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }
}

public sealed class MenuReadModelDocument
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<RelatedNewsSnapshot> News { get; set; } = new();
}

public sealed class NewsReadModelDocument
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<RelatedMenuSnapshot> Menus { get; set; } = new();
}