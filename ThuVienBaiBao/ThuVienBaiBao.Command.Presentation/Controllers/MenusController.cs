using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThuVienBaiBao.Command.Application.Menus;
using ThuVienBaiBao.Command.Contracts.Menus;

namespace ThuVienBaiBao.Command.Presentation.Controllers;

[ApiController]
[Route("api/menus")]
public sealed class MenusController : ControllerBase
{
    private readonly IMediator mediator;

    public MenusController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<MenuDetailDto>> Create([FromBody] CreateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateMenuCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.NewsIds), cancellationToken);

        return Created($"/api/menus/{result.Id}", result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuDetailDto>> Update(int id, [FromBody] UpdateMenuRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateMenuCommand(
            id,
            request.Name,
            request.Slug,
            request.Description,
            request.NewsIds), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteMenuCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

