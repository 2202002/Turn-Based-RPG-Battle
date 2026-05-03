using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Services;

namespace RpgGame.Controllers;

[ApiController]
[Route("api/monsters")]
[RequireAuth]
public class MonstersController : ControllerBase
{
    private readonly GameDbContext _db;
    public MonstersController(GameDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<MonsterDto>>> List()
    {
        var monsters = await _db.Monsters.OrderBy(m => m.RunOrder).ToListAsync();
        var moves = await _db.Moves.ToListAsync();
        return Ok(monsters.Select(m => m.ToDto(moves)).ToList());
    }
}
