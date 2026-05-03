using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Models;

namespace RpgGame.Services;

public class RewardService
{
    private readonly GameDbContext _db;
    private readonly Random _rng;

    public const int RewardChoicesCount = 3;

    public RewardService(GameDbContext db)
    {
        _db = db;
        _rng = Random.Shared;
    }

    public async Task<PendingRewardDto?> CreatePendingRewardAsync(Hero hero, Monster monster, List<Move> allMoves)
    {
        var poolMoveIds = Mappers.ParseMoveIds(monster.MoveIdsCsv);
        if (poolMoveIds.Count == 0) return null;

        var weights = await LoadOrInitWeightsAsync(hero.Id, poolMoveIds);

        var picked = WeightedSampleWithoutReplacement(weights, RewardChoicesCount);
        if (picked.Count == 0) return null;

        foreach (var w in picked)
        {
            w.TimesOffered++;
            w.Weight = Math.Max(SkillWeight.MinWeight, w.Weight * SkillWeight.DecayMultiplier);
        }

        var existing = await _db.PendingRewards.Where(p => p.HeroId == hero.Id).ToListAsync();
        _db.PendingRewards.RemoveRange(existing);

        var pending = new PendingReward
        {
            HeroId = hero.Id,
            MonsterId = monster.Id,
            ChoiceMoveIdsCsv = string.Join(",", picked.Select(p => p.MoveId))
        };
        _db.PendingRewards.Add(pending);

        await _db.SaveChangesAsync();

        var heroMovesByMoveId = hero.HeroMoves.ToDictionary(hm => hm.MoveId);
        var choiceDtos = picked.Select(w =>
        {
            var move = allMoves.First(m => m.Id == w.MoveId);
            heroMovesByMoveId.TryGetValue(w.MoveId, out var existingHm);
            int? currentLevel = existingHm?.UpgradeLevel;
            bool atMax = existingHm != null && existingHm.UpgradeLevel >= MoveStats.MaxUpgradeLevel;
            int? nextLevel = (existingHm == null || atMax) ? null : existingHm.UpgradeLevel + 1;

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
                Weight: w.Weight
            );
        }).ToList();

        return new PendingRewardDto(monster.Id, monster.Name, choiceDtos);
    }

    public async Task<ChooseRewardResponse> ChooseAsync(int userId, int heroId, ChooseRewardRequest req)
    {
        var hero = await _db.Heroes
            .Include(h => h.HeroMoves).ThenInclude(hm => hm.Move)
            .FirstOrDefaultAsync(h => h.Id == heroId && h.UserId == userId)
            ?? throw new KeyNotFoundException("Hero not found.");

        var pending = await _db.PendingRewards
            .Include(p => p.Monster)
            .FirstOrDefaultAsync(p => p.HeroId == heroId)
            ?? throw new InvalidOperationException("No pending reward to choose from.");

        var allowedIds = Mappers.ParseMoveIds(pending.ChoiceMoveIdsCsv);
        if (!allowedIds.Contains(req.MoveId))
            throw new ArgumentException("That move was not among the offered choices.");

        var move = await _db.Moves.FirstOrDefaultAsync(m => m.Id == req.MoveId)
            ?? throw new KeyNotFoundException("Move not found.");

        MoveDto? learnedMoveDto = null;
        HeroMoveDto? upgradedMoveDto = null;
        bool maxedOut = false;

        var existingHm = hero.HeroMoves.FirstOrDefault(hm => hm.MoveId == req.MoveId);
        if (existingHm == null)
        {
            // Throw before consuming the pending reward so the player can drop a move and retry.
            if (hero.HeroMoves.Count >= HeroService.MaxLearnedMoves)
                throw new InvalidOperationException(
                    $"Your hero already knows the maximum {HeroService.MaxLearnedMoves} moves. " +
                    $"Drop one before learning a new move, or pick a move you already know to upgrade it.");

            var newHm = new HeroMove
            {
                HeroId = hero.Id,
                MoveId = move.Id,
                IsEquipped = false,
                UpgradeLevel = 0
            };
            hero.HeroMoves.Add(newHm);
            _db.HeroMoves.Add(newHm);
            learnedMoveDto = move.ToDto();
        }
        else if (existingHm.UpgradeLevel >= MoveStats.MaxUpgradeLevel)
        {
            maxedOut = true;
        }
        else
        {
            existingHm.UpgradeLevel++;
            upgradedMoveDto = existingHm.ToDto();
        }

        _db.PendingRewards.Remove(pending);
        await _db.SaveChangesAsync();

        return new ChooseRewardResponse(
            Hero: hero.ToDto(),
            LearnedMove: learnedMoveDto,
            UpgradedMove: upgradedMoveDto,
            MaxedOut: maxedOut
        );
    }

    private async Task<List<SkillWeight>> LoadOrInitWeightsAsync(int heroId, List<int> moveIds)
    {
        var existing = await _db.SkillWeights
            .Where(sw => sw.HeroId == heroId && moveIds.Contains(sw.MoveId))
            .ToListAsync();
        var existingByMoveId = existing.ToDictionary(sw => sw.MoveId);

        var result = new List<SkillWeight>();
        foreach (var mid in moveIds)
        {
            if (existingByMoveId.TryGetValue(mid, out var sw))
            {
                result.Add(sw);
            }
            else
            {
                var fresh = new SkillWeight { HeroId = heroId, MoveId = mid, Weight = SkillWeight.InitialWeight };
                _db.SkillWeights.Add(fresh);
                result.Add(fresh);
            }
        }
        return result;
    }

    private List<SkillWeight> WeightedSampleWithoutReplacement(List<SkillWeight> source, int count)
    {
        var pool = source.ToList();
        var result = new List<SkillWeight>();
        int target = Math.Min(count, pool.Count);

        for (int i = 0; i < target; i++)
        {
            double total = pool.Sum(p => p.Weight);
            if (total <= 0)
            {
                var idx = _rng.Next(pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
                continue;
            }

            double roll = _rng.NextDouble() * total;
            double acc = 0;
            int pickedIdx = pool.Count - 1;
            for (int j = 0; j < pool.Count; j++)
            {
                acc += pool[j].Weight;
                if (roll <= acc)
                {
                    pickedIdx = j;
                    break;
                }
            }
            result.Add(pool[pickedIdx]);
            pool.RemoveAt(pickedIdx);
        }
        return result;
    }
}
