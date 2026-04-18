using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThuVienBaiBao.Command.Application.News;
using ThuVienBaiBao.Command.Contracts.News;

namespace ThuVienBaiBao.Command.Presentation.Controllers;

[ApiController]
[Route("api/news")]
public sealed class NewsController : ControllerBase
{
    private readonly IMediator mediator;

    public NewsController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<NewsDetailDto>> Create([FromBody] CreateNewsRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateNewsCommand(
            request.Title,
            request.Slug,
            request.Content,
            request.IsPublished,
            request.MenuIds), cancellationToken);

        return Created($"/api/news/{result.Id}", result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<NewsDetailDto>> Update(int id, [FromBody] UpdateNewsRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateNewsCommand(
            id,
            request.Title,
            request.Slug,
            request.Content,
            request.IsPublished,
            request.MenuIds), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteNewsCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

