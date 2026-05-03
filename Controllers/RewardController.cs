using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Models;
using RpgGame.Services;

namespace RpgGame.Controllers;

[ApiController]
[Route("api/heroes/{heroId:int}/reward")]
[RequireAuth]
public class RewardController : ControllerBase
{
    private readonly RewardService _rewards;
    private readonly GameDbContext _db;

    public RewardController(RewardService rewards, GameDbContext db)
    {
        _rewards = rewards;
        _db = db;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<PendingRewardDto>> Pending(int heroId)
    {
        var user = HttpContext.GetUser();
        var hero = await _db.Heroes
            .Include(h => h.HeroMoves).ThenInclude(hm => hm.Move)
            .FirstOrDefaultAsync(h => h.Id == heroId && h.UserId == user.Id)
            ?? throw new KeyNotFoundException("Hero not found.");

        var pending = await _db.PendingRewards
            .Include(p => p.Monster)
            .FirstOrDefaultAsync(p => p.HeroId == heroId)
            ?? throw new KeyNotFoundException("No pending reward.");

        var allMoves = await _db.Moves.ToListAsync();
        var weights = await _db.SkillWeights
            .Where(sw => sw.HeroId == heroId)
            .ToDictionaryAsync(sw => sw.MoveId, sw => sw.Weight);

        var pickedIds = Mappers.ParseMoveIds(pending.ChoiceMoveIdsCsv);
        var heroMovesByMoveId = hero.HeroMoves.ToDictionary(hm => hm.MoveId);

        var choices = pickedIds.Select(id =>
        {
            var move = allMoves.First(m => m.Id == id);
            heroMovesByMoveId.TryGetValue(id, out var existingHm);
            int? currentLevel = existingHm?.UpgradeLevel;
            bool atMax = existingHm != null && existingHm.UpgradeLevel >= MoveStats.MaxUpgradeLevel;
            int? nextLevel = (existingHm == null || atMax) ? null : existingHm.UpgradeLevel + 1;
            weights.TryGetValue(id, out var w);

            return new RewardChoiceDto(
                MoveId: move.Id,
                Name: move.Name,
                Description: move.Description,
                Category: move.Category.ToString(),
                Archetype: move.Archetype.ToString(),
                BasePower: move.Power,
                BaseAccuracy: move.Accuracy,
                BaseManaCost: move.ManaCost,
                AlreadyKnown: existingHm != null,
                CurrentUpgradeLevel: currentLevel,
                NextUpgradeLevel: nextLevel,
                IsAtMax: atMax,
                Weight: w
            );
        }).ToList();

        return Ok(new PendingRewardDto(pending.MonsterId, pending.Monster.Name, choices));
    }

    [HttpPost("choose")]
    public async Task<ActionResult<ChooseRewardResponse>> Choose(int heroId, [FromBody] ChooseRewardRequest req)
    {
        var user = HttpContext.GetUser();
        return Ok(await _rewards.ChooseAsync(user.Id, heroId, req));
    }
}
