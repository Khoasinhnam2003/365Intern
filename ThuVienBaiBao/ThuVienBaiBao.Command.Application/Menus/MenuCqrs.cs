using MediatR;
using Microsoft.EntityFrameworkCore;
using ThuVienBaiBao.Command.Application.Common.Interfaces;
using ThuVienBaiBao.Command.Contracts.Integration;
using ThuVienBaiBao.Command.Contracts.Menus;
using ThuVienBaiBao.Command.Domain.Entities;

namespace ThuVienBaiBao.Command.Application.Menus;

public sealed record GetMenusQuery : IRequest<IReadOnlyCollection<MenuSummaryDto>>;

public sealed record GetMenuByIdQuery(int Id) : IRequest<MenuDetailDto?>;

public sealed record CreateMenuCommand(
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<int>? NewsIds) : IRequest<MenuDetailDto>;

public sealed record UpdateMenuCommand(
    int Id,
    string Name,
    string Slug,
    string? Description,
    IReadOnlyCollection<int>? NewsIds) : IRequest<MenuDetailDto?>;

public sealed record DeleteMenuCommand(int Id) : IRequest<bool>;

public sealed class GetMenusQueryHandler : IRequestHandler<GetMenusQuery, IReadOnlyCollection<MenuSummaryDto>>
{
    private readonly IApplicationDbContext context;

    public GetMenusQueryHandler(IApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyCollection<MenuSummaryDto>> Handle(GetMenusQuery request, CancellationToken cancellationToken)
    {
        return await context.Menus
            .AsNoTracking()
            .OrderBy(menu => menu.Name)
            .Select(menu => new MenuSummaryDto(
                menu.Id,
                menu.Name,
                menu.Slug,
                menu.Description,
                menu.MenuNews.Count))
            .ToListAsync(cancellationToken);
    }
}

public sealed class GetMenuByIdQueryHandler : IRequestHandler<GetMenuByIdQuery, MenuDetailDto?>
{
    private readonly IApplicationDbContext context;

    public GetMenuByIdQueryHandler(IApplicationDbContext context)
    {
        this.context = context;
    }

    public async Task<MenuDetailDto?> Handle(GetMenuByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Menus
            .AsNoTracking()
            .Where(menu => menu.Id == request.Id)
            .Select(menu => new MenuDetailDto(
                menu.Id,
                menu.Name,
                menu.Slug,
                menu.Description,
                menu.MenuNews
                    .OrderBy(link => link.News.Title)
                    .Select(link => new MenuRelatedNewsDto(link.News.Id, link.News.Title, link.News.Slug))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, MenuDetailDto>
{
    private readonly IApplicationDbContext context;
    private readonly IIntegrationEventPublisher integrationEventPublisher;

    public CreateMenuCommandHandler(IApplicationDbContext context, IIntegrationEventPublisher integrationEventPublisher)
    {
        this.context = context;
        this.integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<MenuDetailDto> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = new Menu
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await context.Menus.AddAsync(menu, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SyncNewsAsync(menu.Id, request.NewsIds, cancellationToken);

        var result = await GetMenuById(menu.Id, cancellationToken) ?? throw new InvalidOperationException("Menu was not created.");

        await integrationEventPublisher.PublishMenuUpsertedAsync(
            new MenuUpsertedIntegrationEvent(
                result.Id,
                result.Name,
                result.Slug,
                result.Description,
                menu.CreatedAt,
                menu.UpdatedAt,
                result.News.Select(item => new RelatedNewsSnapshot(item.Id, item.Title, item.Slug)).ToList()),
            cancellationToken);

        return result;
    }

    private async Task SyncNewsAsync(int menuId, IReadOnlyCollection<int>? newsIds, CancellationToken cancellationToken)
    {
        var existingLinks = await context.MenuNews
            .Where(link => link.MenuId == menuId)
            .ToListAsync(cancellationToken);

        context.MenuNews.RemoveRange(existingLinks);

        if (newsIds is { Count: > 0 })
        {
            var validNewsIds = await context.News
                .Where(news => newsIds.Contains(news.Id))
                .Select(news => news.Id)
                .ToListAsync(cancellationToken);

            foreach (var newsId in validNewsIds)
            {
                await context.MenuNews.AddAsync(new MenuNews
                {
                    MenuId = menuId,
                    NewsId = newsId
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<MenuDetailDto?> GetMenuById(int id, CancellationToken cancellationToken)
    {
        return await context.Menus
            .AsNoTracking()
            .Where(menu => menu.Id == id)
            .Select(menu => new MenuDetailDto(
                menu.Id,
                menu.Name,
                menu.Slug,
                menu.Description,
                menu.MenuNews
                    .OrderBy(link => link.News.Title)
                    .Select(link => new MenuRelatedNewsDto(link.News.Id, link.News.Title, link.News.Slug))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class UpdateMenuCommandHandler : IRequestHandler<UpdateMenuCommand, MenuDetailDto?>
{
    private readonly IApplicationDbContext context;
    private readonly IIntegrationEventPublisher integrationEventPublisher;

    public UpdateMenuCommandHandler(IApplicationDbContext context, IIntegrationEventPublisher integrationEventPublisher)
    {
        this.context = context;
        this.integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<MenuDetailDto?> Handle(UpdateMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await context.Menus.FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (menu is null)
        {
            return null;
        }

        menu.Name = request.Name;
        menu.Slug = request.Slug;
        menu.Description = request.Description;
        menu.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        await SyncNewsAsync(menu.Id, request.NewsIds, cancellationToken);

        var result = await GetMenuById(menu.Id, cancellationToken);

        if (result is not null)
        {
            await integrationEventPublisher.PublishMenuUpsertedAsync(
                new MenuUpsertedIntegrationEvent(
                    result.Id,
                    result.Name,
                    result.Slug,
                    result.Description,
                    menu.CreatedAt,
                    menu.UpdatedAt,
                    result.News.Select(item => new RelatedNewsSnapshot(item.Id, item.Title, item.Slug)).ToList()),
                cancellationToken);
        }

        return result;
    }

    private async Task SyncNewsAsync(int menuId, IReadOnlyCollection<int>? newsIds, CancellationToken cancellationToken)
    {
        var existingLinks = await context.MenuNews
            .Where(link => link.MenuId == menuId)
            .ToListAsync(cancellationToken);

        context.MenuNews.RemoveRange(existingLinks);

        if (newsIds is { Count: > 0 })
        {
            var validNewsIds = await context.News
                .Where(news => newsIds.Contains(news.Id))
                .Select(news => news.Id)
                .ToListAsync(cancellationToken);

            foreach (var newsId in validNewsIds)
            {
                await context.MenuNews.AddAsync(new MenuNews
                {
                    MenuId = menuId,
                    NewsId = newsId
                }, cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<MenuDetailDto?> GetMenuById(int id, CancellationToken cancellationToken)
    {
        return await context.Menus
            .AsNoTracking()
            .Where(menu => menu.Id == id)
            .Select(menu => new MenuDetailDto(
                menu.Id,
                menu.Name,
                menu.Slug,
                menu.Description,
                menu.MenuNews
                    .OrderBy(link => link.News.Title)
                    .Select(link => new MenuRelatedNewsDto(link.News.Id, link.News.Title, link.News.Slug))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed class DeleteMenuCommandHandler : IRequestHandler<DeleteMenuCommand, bool>
{
    private readonly IApplicationDbContext context;
    private readonly IIntegrationEventPublisher integrationEventPublisher;

    public DeleteMenuCommandHandler(IApplicationDbContext context, IIntegrationEventPublisher integrationEventPublisher)
    {
        this.context = context;
        this.integrationEventPublisher = integrationEventPublisher;
    }

    public async Task<bool> Handle(DeleteMenuCommand request, CancellationToken cancellationToken)
    {
        var menu = await context.Menus
            .Include(item => item.MenuNews)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (menu is null)
        {
            return false;
        }

        var relatedNewsIds = menu.MenuNews.Select(link => link.NewsId).ToList();

        context.Menus.Remove(menu);
        await context.SaveChangesAsync(cancellationToken);

        await integrationEventPublisher.PublishMenuDeletedAsync(
            new MenuDeletedIntegrationEvent(menu.Id, relatedNewsIds),
            cancellationToken);

        return true;
    }
}

