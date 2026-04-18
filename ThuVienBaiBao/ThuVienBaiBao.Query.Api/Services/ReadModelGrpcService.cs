using Grpc.Core;
using ThuVienBaiBao.Query.Application.Common.Interfaces;
using ThuVienBaiBao.Grpc;

namespace ThuVienBaiBao.Query.Api.Services;

public sealed class ReadModelGrpcService : ReadModel.ReadModelBase
{
    private readonly IReadModelStore readModelStore;

    public ReadModelGrpcService(IReadModelStore readModelStore)
    {
        this.readModelStore = readModelStore;
    }

    public override async Task<MenuSummaryListReply> ListMenus(EmptyRequest request, ServerCallContext context)
    {
        var items = await readModelStore.GetMenusAsync(context.CancellationToken);

        var reply = new MenuSummaryListReply();
        reply.Items.AddRange(items.Select(item => new MenuSummaryReply
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug,
            Description = item.Description ?? string.Empty,
            NewsCount = item.NewsCount
        }));

        return reply;
    }

    public override async Task<MenuDetailReply> GetMenuById(MenuRequest request, ServerCallContext context)
    {
        var menu = await readModelStore.GetMenuByIdAsync(request.Id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Menu {request.Id} was not found."));

        var reply = new MenuDetailReply
        {
            Id = menu.Id,
            Name = menu.Name,
            Slug = menu.Slug,
            Description = menu.Description ?? string.Empty
        };

        reply.News.AddRange(menu.News.Select(item => new RelatedNewsReply
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug
        }));

        return reply;
    }

    public override async Task<NewsSummaryListReply> ListNews(EmptyRequest request, ServerCallContext context)
    {
        var items = await readModelStore.GetNewsAsync(context.CancellationToken);

        var reply = new NewsSummaryListReply();
        reply.Items.AddRange(items.Select(item => new NewsSummaryReply
        {
            Id = item.Id,
            Title = item.Title,
            Slug = item.Slug,
            IsPublished = item.IsPublished,
            MenuCount = item.MenuCount
        }));

        return reply;
    }

    public override async Task<NewsDetailReply> GetNewsById(NewsRequest request, ServerCallContext context)
    {
        var news = await readModelStore.GetNewsByIdAsync(request.Id, context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"News {request.Id} was not found."));

        var reply = new NewsDetailReply
        {
            Id = news.Id,
            Title = news.Title,
            Slug = news.Slug,
            Content = news.Content,
            IsPublished = news.IsPublished,
            PublishedAt = news.PublishedAt?.ToString("O") ?? string.Empty
        };

        reply.Menus.AddRange(news.Menus.Select(item => new RelatedMenuReply
        {
            Id = item.Id,
            Name = item.Name,
            Slug = item.Slug
        }));

        return reply;
    }
}