using Microsoft.AspNetCore.Mvc;
using RpgGame.Dtos;
using RpgGame.Services;

namespace RpgGame.Controllers;

[ApiController]
[Route("api/heroes")]
[RequireAuth]
public class HeroesController : ControllerBase
{
    private readonly HeroService _heroes;
    public HeroesController(HeroService heroes) => _heroes = heroes;

    [HttpGet]
    public async Task<ActionResult<List<HeroDto>>> List()
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.GetHeroesForUserAsync(user.Id));
    }

    [HttpPost]
    public async Task<ActionResult<HeroDto>> Create([FromBody] CreateHeroRequest req)
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.CreateHeroAsync(user.Id, req));
    }

    [HttpGet("{heroId:int}")]
    public async Task<ActionResult<HeroDto>> Get(int heroId)
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.GetHeroAsync(user.Id, heroId));
    }

    [HttpPut("{heroId:int}/moves")]
    public async Task<ActionResult<HeroDto>> EquipMoves(int heroId, [FromBody] EquipMovesRequest req)
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.EquipMovesAsync(user.Id, heroId, req));
    }

    [HttpDelete("{heroId:int}/moves/{heroMoveId:int}")]
    public async Task<ActionResult<HeroDto>> DropMove(int heroId, int heroMoveId)
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.DropMoveAsync(user.Id, heroId, heroMoveId));
    }

    [HttpPost("{heroId:int}/run/restart")]
    public async Task<ActionResult<HeroDto>> RestartRun(int heroId)
    {
        var user = HttpContext.GetUser();
        return Ok(await _heroes.RestartRunAsync(user.Id, heroId));
    }
}
