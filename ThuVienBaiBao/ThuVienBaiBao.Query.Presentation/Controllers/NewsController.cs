using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThuVienBaiBao.Query.Application.News;
using ThuVienBaiBao.Query.Contracts.News;

namespace ThuVienBaiBao.Query.Presentation.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly IMediator mediator;

    public NewsController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NewsSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNewsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetNewsByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}

