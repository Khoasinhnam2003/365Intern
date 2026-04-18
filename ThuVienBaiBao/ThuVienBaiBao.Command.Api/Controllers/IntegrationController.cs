using Microsoft.AspNetCore.Mvc;
using ThuVienBaiBao.Grpc;

namespace ThuVienBaiBao.Command.Api.Controllers;

[ApiController]
[Route("api/integration")]
public sealed class IntegrationController : ControllerBase
{
    private readonly ReadModel.ReadModelClient readModelClient;

    public IntegrationController(ReadModel.ReadModelClient readModelClient)
    {
        this.readModelClient = readModelClient;
    }

    [HttpGet("menus/{id:int}")]
    public async Task<ActionResult<MenuDetailReply>> GetMenuById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await readModelClient.GetMenuByIdAsync(new MenuRequest { Id = id }, cancellationToken: cancellationToken);
            return Ok(result);
        }
        catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound();
        }
    }

    [HttpGet("news/{id:int}")]
    public async Task<ActionResult<NewsDetailReply>> GetNewsById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await readModelClient.GetNewsByIdAsync(new NewsRequest { Id = id }, cancellationToken: cancellationToken);
            return Ok(result);
        }
        catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            return NotFound();
        }
    }
}