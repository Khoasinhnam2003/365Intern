using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThuVienBaiBao.Query.Application.Menus;
using ThuVienBaiBao.Query.Contracts.Menus;

namespace ThuVienBaiBao.Query.Presentation.Controllers;

[ApiController]
[Route("api/menus")]
public sealed class MenusController : ControllerBase
{
    private readonly IMediator mediator;

    public MenusController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MenuSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMenusQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMenuByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}

