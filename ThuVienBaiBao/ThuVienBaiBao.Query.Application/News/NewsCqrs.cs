using MediatR;
using Microsoft.EntityFrameworkCore;
using ThuVienBaiBao.Query.Application.Common.Interfaces;
using ThuVienBaiBao.Query.Contracts.News;
using DomainMenuNews = ThuVienBaiBao.Query.Domain.Entities.MenuNews;
using DomainNews = ThuVienBaiBao.Query.Domain.Entities.News;

namespace ThuVienBaiBao.Query.Application.News;

public sealed record GetNewsQuery : IRequest<IReadOnlyCollection<NewsSummaryDto>>;

public sealed record GetNewsByIdQuery(int Id) : IRequest<NewsDetailDto?>;

public sealed record CreateNewsCommand(
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    IReadOnlyCollection<int>? MenuIds) : IRequest<NewsDetailDto>;

public sealed record UpdateNewsCommand(
    int Id,
    string Title,
    string Slug,
    string Content,
    bool IsPublished,
    IReadOnlyCollection<int>? MenuIds) : IRequest<NewsDetailDto?>;

public sealed record DeleteNewsCommand(int Id) : IRequest<bool>;

public sealed class GetNewsQueryHandler : IRequestHandler<GetNewsQuery, IReadOnlyCollection<NewsSummaryDto>>
{
    private readonly IReadModelStore readModelStore;

    public GetNewsQueryHandler(IReadModelStore readModelStore)
    {
        this.readModelStore = readModelStore;
    }

    public async Task<IReadOnlyCollection<NewsSummaryDto>> Handle(GetNewsQuery request, CancellationToken cancellationToken)
    {
        return await readModelStore.GetNewsAsync(cancellationToken);
    }
}

public sealed class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, NewsDetailDto?>
{
    private readonly IReadModelStore readModelStore;

    public GetNewsByIdQueryHandler(IReadModelStore readModelStore)
    {
        this.readModelStore = readModelStore;
    }

    public async Task<NewsDetailDto?> Handle(GetNewsByIdQuery request, CancellationToken cancellationToken)
    {
        return await readModelStore.GetNewsByIdAsync(request.Id, cancellationToken);
    }
}

public sealed class CreateNewsCommandHandler : IRequestHandler<CreateNewsCommand, NewsDetailDto>
{
    private readonly IApplicationDbContext context;

    public CreateNewsCommandHandler(IApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<NewsDetailDto> Handle(CreateNewsCommand request, CancellationToken cancellationToken)
    {
        var news = new DomainNews
        {
            Title = request.Title,
            Slug = request.Slug,
            Content = request.Content,
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        await context.News.AddAsync(news, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SyncMenusAsync(news.Id, request.MenuIds, cancellationToken);

        return await GetNewsById(news.Id, cancellationToken) ?? throw new InvalidOperationException("News was not created.");
    }

    private async Task SyncMenusAsync(int newsId, IReadOnlyCollection<int>? menuIds, CancellationToken cancellationToken)
    {
        var existingLinks = await context.MenuNews
            .Where(link => link.NewsId == newsId)
            .ToListAsync(cancellationToken);

        context.MenuNews.RemoveRange(existingLinks);

        if (menuIds is { Count: > 0 })
        {
            var validMenuIds = await context.Menus
                .Where(menu => menuIds.Contains(menu.Id))
                .Select(menu => menu.Id)
                .ToListAsync(cancellationToken);

            foreach (var menuId in validMenuIds)
            {
                await context.MenuNews.AddAsync(new DomainMenuNews
                {
                    MenuId = menuId,
                    NewsId = newsId
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<NewsDetailDto?> GetNewsById(int id, CancellationToken cancellationToken)
    {
        return await context.News
            .AsNoTracking()
            .Where(news => news.Id == id)
            .Select(news => new NewsDetailDto(
                news.Id,
                news.Title,
                news.Slug,
                news.Content,
                news.IsPublished,
                news.PublishedAt,
                news.MenuNews
                    .OrderBy(link => link.Menu.Name)
                    .Select(link => new NewsRelatedMenuDto(link.Menu.Id, link.Menu.Name, link.Menu.Slug))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class UpdateNewsCommandHandler : IRequestHandler<UpdateNewsCommand, NewsDetailDto?>
{
    private readonly IApplicationDbContext context;

    public UpdateNewsCommandHandler(IApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<NewsDetailDto?> Handle(UpdateNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await context.News.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (news is null)
        {
            return null;
        }

        news.Title = request.Title;
        news.Slug = request.Slug;
        news.Content = request.Content;
        news.IsPublished = request.IsPublished;
        news.PublishedAt = request.IsPublished ? news.PublishedAt ?? DateTime.UtcNow : null;
        news.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        await SyncMenusAsync(news.Id, request.MenuIds, cancellationToken);

        return await GetNewsById(news.Id, cancellationToken);
    }

    private async Task SyncMenusAsync(int newsId, IReadOnlyCollection<int>? menuIds, CancellationToken cancellationToken)
    {
        var existingLinks = await context.MenuNews
            .Where(link => link.NewsId == newsId)
            .ToListAsync(cancellationToken);

        context.MenuNews.RemoveRange(existingLinks);

        if (menuIds is { Count: > 0 })
        {
            var validMenuIds = await context.Menus
                .Where(menu => menuIds.Contains(menu.Id))
                .Select(menu => menu.Id)
                .ToListAsync(cancellationToken);

            foreach (var menuId in validMenuIds)
            {
                await context.MenuNews.AddAsync(new DomainMenuNews
                {
                    MenuId = menuId,
                    NewsId = newsId
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<NewsDetailDto?> GetNewsById(int id, CancellationToken cancellationToken)
    {
        return await context.News
            .AsNoTracking()
            .Where(news => news.Id == id)
            .Select(news => new NewsDetailDto(
                news.Id,
                news.Title,
                news.Slug,
                news.Content,
                news.IsPublished,
                news.PublishedAt,
                news.MenuNews
                    .OrderBy(link => link.Menu.Name)
                    .Select(link => new NewsRelatedMenuDto(link.Menu.Id, link.Menu.Name, link.Menu.Slug))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, bool>
{
    private readonly IApplicationDbContext context;

    public DeleteNewsCommandHandler(IApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<bool> Handle(DeleteNewsCommand request, CancellationToken cancellationToken)
    {
        var news = await context.News.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (news is null)
        {
            return false;
        }

        context.News.Remove(news);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

