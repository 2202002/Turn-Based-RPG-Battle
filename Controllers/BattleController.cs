using Microsoft.AspNetCore.Mvc;
using RpgGame.Dtos;
using RpgGame.Services;

namespace RpgGame.Controllers;

[ApiController]
[Route("api/heroes/{heroId:int}/battle")]
[RequireAuth]
public class BattleController : ControllerBase
{
    private readonly BattleService _battles;
    public BattleController(BattleService battles) => _battles = battles;

    [HttpPost("start")]
    public async Task<ActionResult<BattleStateDto>> Start(int heroId, [FromBody] StartBattleRequest? req)
    {
        var user = HttpContext.GetUser();
        return Ok(await _battles.StartBattleAsync(user.Id, heroId, req ?? new StartBattleRequest(null)));
    }

    [HttpGet("active")]
    public async Task<ActionResult<BattleStateDto>> Active(int heroId)
    {
        var user = HttpContext.GetUser();
        return Ok(await _battles.GetActiveBattleAsync(user.Id, heroId));
    }

    [HttpPost("action")]
    public async Task<ActionResult<BattleActionResponse>> Action(int heroId, [FromBody] BattleActionRequest req)
    {
        var user = HttpContext.GetUser();
        return Ok(await _battles.PerformActionAsync(user.Id, heroId, req));
    }
}
