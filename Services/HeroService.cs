using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Models;

namespace RpgGame.Services;

public class HeroService
{
    private readonly GameDbContext _db;

    public const int MaxEquippedMoves = 4;
    public const int MaxLearnedMoves = 10;
    public const int MinLearnedMoves = 1;
    public const double DropWeightPenaltyMultiplier = 0.5;

    private static readonly int[] DefaultStartingMoveIds = new[] { 1, 2 };

    public HeroService(GameDbContext db) => _db = db;

    public async Task<List<HeroDto>> GetHeroesForUserAsync(int userId)
    {
        var heroes = await _db.Heroes
            .Where(h => h.UserId == userId)
            .Include(h => h.HeroMoves).ThenInclude(hm => hm.Move)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
        return heroes.Select(h => h.ToDto()).ToList();
    }

    public async Task<HeroDto> GetHeroAsync(int userId, int heroId)
    {
        var hero = await LoadHeroAsync(userId, heroId);
        return hero.ToDto();
    }

    public async Task<HeroDto> CreateHeroAsync(int userId, CreateHeroRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Hero name is required.");
        if (req.Name.Length > 32)
            throw new ArgumentException("Hero name too long.");

        var hero = new Hero
        {
            UserId = userId,
            Name = req.Name.Trim(),
            Level = 1,
            Xp = 0,
            RunProgress = 0,
            RunCompleted = false
        };
        StatsCalculator.ApplyStatsForLevel(hero);

        foreach (var mid in DefaultStartingMoveIds)
        {
            hero.HeroMoves.Add(new HeroMove
            {
                MoveId = mid,
                IsEquipped = true
            });
        }

        _db.Heroes.Add(hero);
        await _db.SaveChangesAsync();

        return await GetHeroAsync(userId, hero.Id);
    }

    public async Task<HeroDto> EquipMovesAsync(int userId, int heroId, EquipMovesRequest req)
    {
        if (req.EquippedHeroMoveIds.Count > MaxEquippedMoves)
            throw new ArgumentException($"You can equip at most {MaxEquippedMoves} moves.");
        if (req.EquippedHeroMoveIds.Count == 0)
            throw new ArgumentException("You must equip at least one move.");
        if (req.EquippedHeroMoveIds.Distinct().Count() != req.EquippedHeroMoveIds.Count)
            throw new ArgumentException("Duplicate move IDs in equipped set.");

        var hero = await LoadHeroAsync(userId, heroId);

        var idSet = req.EquippedHeroMoveIds.ToHashSet();
        var ownedIds = hero.HeroMoves.Select(hm => hm.Id).ToHashSet();
        if (!idSet.IsSubsetOf(ownedIds))
            throw new ArgumentException("One or more moves don't belong to this hero.");

        foreach (var hm in hero.HeroMoves)
            hm.IsEquipped = idSet.Contains(hm.Id);

        await _db.SaveChangesAsync();
        return hero.ToDto();
    }

    public async Task<HeroDto> DropMoveAsync(int userId, int heroId, int heroMoveId)
    {
        var hero = await LoadHeroAsync(userId, heroId);

        var heroMove = hero.HeroMoves.FirstOrDefault(hm => hm.Id == heroMoveId)
            ?? throw new KeyNotFoundException("Move not owned by hero.");

        if (heroMove.IsEquipped)
            throw new InvalidOperationException("Unequip the move before dropping it.");

        if (hero.HeroMoves.Count <= MinLearnedMoves)
            throw new InvalidOperationException($"You must keep at least {MinLearnedMoves} move(s).");

        // Halving the weight makes a dropped move show up less often in future picks.
        var weight = await _db.SkillWeights
            .FirstOrDefaultAsync(sw => sw.HeroId == hero.Id && sw.MoveId == heroMove.MoveId);
        if (weight != null)
        {
            weight.Weight = Math.Max(SkillWeight.MinWeight, weight.Weight * DropWeightPenaltyMultiplier);
        }

        _db.HeroMoves.Remove(heroMove);
        hero.HeroMoves.Remove(heroMove);

        await _db.SaveChangesAsync();
        return hero.ToDto();
    }

    public async Task<HeroDto> RestartRunAsync(int userId, int heroId)
    {
        var hero = await LoadHeroAsync(userId, heroId);
        hero.RunProgress = 0;
        hero.RunCompleted = false;

        var active = await _db.BattleSessions
            .Where(b => b.HeroId == heroId && !b.IsFinished)
            .ToListAsync();
        foreach (var b in active)
        {
            b.IsFinished = true;
            b.HeroWon = false;
        }

        await _db.SaveChangesAsync();
        return hero.ToDto();
    }

    public List<BattleEventDto> AwardXpAndLevelUp(Hero hero, int xpGained)
    {
        var events = new List<BattleEventDto>();
        hero.Xp += xpGained;

        while (hero.Xp >= StatsCalculator.XpToNextLevel(hero.Level))
        {
            hero.Xp -= StatsCalculator.XpToNextLevel(hero.Level);
            hero.Level++;
            StatsCalculator.ApplyStatsForLevel(hero);
            events.Add(new BattleEventDto(
                Actor: "hero",
                Type: "level_up",
                Message: $"{hero.Name} reached level {hero.Level}!"
            ));
        }
        return events;
    }

    private async Task<Hero> LoadHeroAsync(int userId, int heroId)
    {
        var hero = await _db.Heroes
            .Include(h => h.HeroMoves).ThenInclude(hm => hm.Move)
            .FirstOrDefaultAsync(h => h.Id == heroId && h.UserId == userId);
        return hero ?? throw new KeyNotFoundException("Hero not found.");
    }
}
