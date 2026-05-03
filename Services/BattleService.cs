using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Models;

namespace RpgGame.Services;

public class BattleService
{
    private readonly GameDbContext _db;
    private readonly HeroService _heroService;
    private readonly RewardService _rewardService;
    private readonly Random _rng;

    public const int MonstersPerRun = 5;

    public BattleService(GameDbContext db, HeroService heroService, RewardService rewardService)
    {
        _db = db;
        _heroService = heroService;
        _rewardService = rewardService;
        _rng = Random.Shared;
    }

    public async Task<BattleStateDto> StartBattleAsync(int userId, int heroId, StartBattleRequest req)
    {
        var hero = await LoadHeroAsync(userId, heroId);

        var existing = await _db.BattleSessions
            .FirstOrDefaultAsync(b => b.HeroId == heroId && !b.IsFinished);
        if (existing != null)
            throw new InvalidOperationException("There is already an active battle. Finish it before starting a new one.");

        if (!hero.HeroMoves.Any(hm => hm.IsEquipped))
            throw new InvalidOperationException("Hero has no equipped moves.");

        Monster monster;
        if (req.MonsterId is int explicitId)
        {
            monster = await _db.Monsters.FirstOrDefaultAsync(m => m.Id == explicitId)
                ?? throw new KeyNotFoundException("Monster not found.");
        }
        else
        {
            if (hero.RunCompleted)
                throw new InvalidOperationException("Run already completed. Restart it or pick a specific monster to replay.");

            var nextOrder = hero.RunProgress + 1;
            if (nextOrder > MonstersPerRun)
                throw new InvalidOperationException("Run already completed.");

            monster = await _db.Monsters.FirstOrDefaultAsync(m => m.RunOrder == nextOrder)
                ?? throw new InvalidOperationException($"No monster found for run order {nextOrder}.");
        }

        var battle = new BattleSession
        {
            HeroId = hero.Id,
            MonsterId = monster.Id,
            HeroHp = hero.MaxHp,
            HeroMana = hero.MaxMana,
            MonsterHp = monster.MaxHp,
            MonsterMana = monster.MaxMana,
            TurnNumber = 1,
            IsFinished = false
        };
        _db.BattleSessions.Add(battle);

        // Starting a new battle implicitly skips any unresolved reward from a previous one.
        var staleReward = await _db.PendingRewards.Where(p => p.HeroId == hero.Id).ToListAsync();
        _db.PendingRewards.RemoveRange(staleReward);

        await _db.SaveChangesAsync();

        return BuildState(battle, hero, monster);
    }

    public async Task<BattleStateDto> GetActiveBattleAsync(int userId, int heroId)
    {
        var hero = await LoadHeroAsync(userId, heroId);
        var battle = await _db.BattleSessions
            .Include(b => b.Monster)
            .FirstOrDefaultAsync(b => b.HeroId == heroId && !b.IsFinished)
            ?? throw new KeyNotFoundException("No active battle for this hero.");
        return BuildState(battle, hero, battle.Monster);
    }

    public async Task<BattleActionResponse> PerformActionAsync(int userId, int heroId, BattleActionRequest req)
    {
        var hero = await LoadHeroAsync(userId, heroId);
        var battle = await _db.BattleSessions
            .Include(b => b.Monster)
            .FirstOrDefaultAsync(b => b.HeroId == heroId && !b.IsFinished)
            ?? throw new InvalidOperationException("No active battle. Start one first.");

        var monster = battle.Monster;
        var allMoves = await _db.Moves.ToListAsync();

        var heroMove = hero.HeroMoves.FirstOrDefault(hm => hm.Id == req.HeroMoveId)
            ?? throw new ArgumentException("Move not owned by hero.");
        if (!heroMove.IsEquipped)
            throw new ArgumentException("Move is not equipped.");

        var events = new List<BattleEventDto>();

        bool monsterFirst = monster.Speed > hero.Speed;

        if (monsterFirst)
        {
            ResolveMonsterTurn(battle, hero, monster, allMoves, events);
            if (battle.HeroHp > 0)
                ResolveHeroTurn(battle, hero, monster, heroMove, events);
        }
        else
        {
            ResolveHeroTurn(battle, hero, monster, heroMove, events);
            if (battle.MonsterHp > 0)
                ResolveMonsterTurn(battle, hero, monster, allMoves, events);
        }

        battle.TurnNumber++;

        HeroDto? updatedHeroDto = null;
        PendingRewardDto? pendingReward = null;

        if (battle.MonsterHp <= 0 && battle.HeroHp <= 0)
        {
            // Simultaneous KO: whoever moved second wins (their final blow lands).
            bool heroWon = !monsterFirst;
            await FinishBattleAsync(battle, hero, monster, heroWon, events);
            if (heroWon)
                pendingReward = await _rewardService.CreatePendingRewardAsync(hero, monster, allMoves);
            updatedHeroDto = hero.ToDto();
        }
        else if (battle.MonsterHp <= 0)
        {
            await FinishBattleAsync(battle, hero, monster, heroWon: true, events);
            pendingReward = await _rewardService.CreatePendingRewardAsync(hero, monster, allMoves);
            updatedHeroDto = hero.ToDto();
        }
        else if (battle.HeroHp <= 0)
        {
            await FinishBattleAsync(battle, hero, monster, heroWon: false, events);
            updatedHeroDto = hero.ToDto();
        }
        else
        {
            await _db.SaveChangesAsync();
        }

        return new BattleActionResponse(
            State: BuildState(battle, hero, monster),
            Events: events,
            UpdatedHero: updatedHeroDto,
            PendingReward: pendingReward
        );
    }

    private void ResolveHeroTurn(BattleSession battle, Hero hero, Monster monster, HeroMove heroMove, List<BattleEventDto> events)
    {
        var move = heroMove.Move;
        var eff = MoveStats.Compute(move, heroMove.UpgradeLevel);

        if (move.Category == MoveCategory.Magic && battle.HeroMana < eff.ManaCost)
        {
            events.Add(new BattleEventDto(
                Actor: "hero", Type: "no_mana",
                Message: $"{hero.Name} tried to use {eff.DisplayName} but didn't have enough mana!",
                HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
                MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana
            ));
            return;
        }

        events.Add(new BattleEventDto(
            Actor: "hero", Type: "move_used",
            Message: $"{hero.Name} used {eff.DisplayName}!"
        ));

        // Mana is spent up-front so a missed magic attack still costs.
        if (move.Category == MoveCategory.Magic && eff.ManaCost > 0)
            battle.HeroMana -= eff.ManaCost;

        if (_rng.Next(0, 100) >= eff.Accuracy)
        {
            events.Add(new BattleEventDto(
                Actor: "hero", Type: "miss",
                Message: $"{hero.Name}'s attack missed!",
                ManaSpent: move.Category == MoveCategory.Magic && eff.ManaCost > 0 ? eff.ManaCost : null,
                HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
                MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana
            ));
            return;
        }

        var dmg = CalculateDamage(
            attackerStat: move.Category == MoveCategory.Physical ? hero.Attack : 0,
            defenderDef: monster.Defense,
            category: move.Category,
            effectivePower: eff.Power
        );

        bool isCrit = false;
        if (eff.MageGuaranteedCrit)
        {
            dmg = (int)Math.Round(dmg * MoveStats.MageCritMultiplier);
            isCrit = true;
        }

        bool isExecute = false;
        if (eff.AssassinExecute)
        {
            double monsterHpFraction = monster.MaxHp > 0
                ? (double)battle.MonsterHp / monster.MaxHp
                : 0;
            if (monsterHpFraction < MoveStats.AssassinExecuteHpThreshold)
            {
                dmg = (int)Math.Round(dmg * MoveStats.AssassinExecuteMultiplier);
                isExecute = true;
            }
        }

        battle.MonsterHp = Math.Max(0, battle.MonsterHp - dmg);

        int healed = 0;
        if (eff.FighterLifesteal && dmg > 0)
        {
            healed = (int)Math.Round(dmg * MoveStats.FighterLifestealFraction);
            if (healed > 0)
            {
                int newHp = Math.Min(hero.MaxHp, battle.HeroHp + healed);
                healed = newHp - battle.HeroHp;
                battle.HeroHp = newHp;
            }
        }

        if (isCrit)
            events.Add(new BattleEventDto(Actor: "hero", Type: "crit", Message: "Critical hit!"));
        if (isExecute)
            events.Add(new BattleEventDto(Actor: "hero", Type: "execute", Message: $"Execute! {move.Name} strikes a wounded foe for massive damage."));

        events.Add(new BattleEventDto(
            Actor: "hero", Type: "damage",
            Message: $"{monster.Name} took {dmg} damage.",
            Damage: dmg,
            ManaSpent: move.Category == MoveCategory.Magic && eff.ManaCost > 0 ? eff.ManaCost : null,
            HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
            MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana,
            IsCrit: isCrit
        ));

        if (healed > 0)
        {
            events.Add(new BattleEventDto(
                Actor: "hero", Type: "lifesteal",
                Message: $"{hero.Name} drained {healed} HP.",
                HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
                MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana
            ));
        }
    }

    private void ResolveMonsterTurn(BattleSession battle, Hero hero, Monster monster, List<Move> allMoves, List<BattleEventDto> events)
    {
        var monsterMoveIds = Mappers.ParseMoveIds(monster.MoveIdsCsv);
        var monsterMoves = allMoves.Where(m => monsterMoveIds.Contains(m.Id)).ToList();

        var affordable = monsterMoves
            .Where(m => m.Category == MoveCategory.Physical || battle.MonsterMana >= m.ManaCost)
            .ToList();
        var pool = affordable.Count > 0 ? affordable : monsterMoves;
        if (pool.Count == 0) return;

        var move = pool[_rng.Next(pool.Count)];

        if (move.Category == MoveCategory.Magic && battle.MonsterMana < move.ManaCost)
        {
            events.Add(new BattleEventDto(
                Actor: "monster", Type: "no_mana",
                Message: $"{monster.Name} tried to use {move.Name} but didn't have enough mana!"
            ));
            return;
        }

        events.Add(new BattleEventDto(
            Actor: "monster", Type: "move_used",
            Message: $"{monster.Name} used {move.Name}!"
        ));

        if (move.Category == MoveCategory.Magic)
            battle.MonsterMana -= move.ManaCost;

        if (_rng.Next(0, 100) >= move.Accuracy)
        {
            events.Add(new BattleEventDto(
                Actor: "monster", Type: "miss",
                Message: $"{monster.Name}'s attack missed!",
                HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
                MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana
            ));
            return;
        }

        var dmg = CalculateDamage(
            attackerStat: move.Category == MoveCategory.Physical ? monster.Attack : 0,
            defenderDef: hero.Defense,
            category: move.Category,
            effectivePower: move.Power
        );
        battle.HeroHp = Math.Max(0, battle.HeroHp - dmg);

        events.Add(new BattleEventDto(
            Actor: "monster", Type: "damage",
            Message: $"{hero.Name} took {dmg} damage.",
            Damage: dmg,
            HeroHpAfter: battle.HeroHp, HeroManaAfter: battle.HeroMana,
            MonsterHpAfter: battle.MonsterHp, MonsterManaAfter: battle.MonsterMana
        ));
    }

    // Physical: (Attack * Power / 10) - (Defense / 2)
    // Magic:    (Power * 1.3)         - (Defense / 3)
    // Final damage is multiplied by a 0.85..1.0 random factor and clamped to a minimum of 1.
    private int CalculateDamage(int attackerStat, int defenderDef, MoveCategory category, int effectivePower)
    {
        double baseDmg;
        if (category == MoveCategory.Physical)
            baseDmg = (attackerStat * effectivePower / 10.0) - (defenderDef / 2.0);
        else
            baseDmg = (effectivePower * 1.3) - (defenderDef / 3.0);

        if (baseDmg < 1) baseDmg = 1;

        double factor = 0.85 + (_rng.NextDouble() * 0.15);
        int finalDmg = (int)Math.Round(baseDmg * factor);
        return Math.Max(1, finalDmg);
    }

    private async Task FinishBattleAsync(BattleSession battle, Hero hero, Monster monster, bool heroWon, List<BattleEventDto> events)
    {
        battle.IsFinished = true;
        battle.HeroWon = heroWon;

        if (heroWon)
        {
            events.Add(new BattleEventDto(
                Actor: "hero", Type: "victory",
                Message: $"{hero.Name} defeated {monster.Name}!"
            ));

            events.AddRange(_heroService.AwardXpAndLevelUp(hero, monster.XpReward));

            // Replays of monsters earlier in the run don't bump RunProgress.
            if (!hero.RunCompleted && monster.RunOrder == hero.RunProgress + 1)
            {
                hero.RunProgress = monster.RunOrder;
                if (hero.RunProgress >= MonstersPerRun)
                {
                    hero.RunCompleted = true;
                    events.Add(new BattleEventDto(
                        Actor: "hero", Type: "run_completed",
                        Message: $"{hero.Name} completed the run!"
                    ));
                }
            }
        }
        else
        {
            events.Add(new BattleEventDto(
                Actor: "monster", Type: "defeat",
                Message: $"{hero.Name} was defeated by {monster.Name}..."
            ));
        }

        await _db.SaveChangesAsync();
    }

    private async Task<Hero> LoadHeroAsync(int userId, int heroId)
    {
        var hero = await _db.Heroes
            .Include(h => h.HeroMoves).ThenInclude(hm => hm.Move)
            .FirstOrDefaultAsync(h => h.Id == heroId && h.UserId == userId);
        return hero ?? throw new KeyNotFoundException("Hero not found.");
    }

    private static BattleStateDto BuildState(BattleSession b, Hero hero, Monster monster)
    {
        return new BattleStateDto(
            BattleId: b.Id,
            HeroId: hero.Id,
            HeroName: hero.Name,
            HeroHp: b.HeroHp,
            HeroMaxHp: hero.MaxHp,
            HeroMana: b.HeroMana,
            HeroMaxMana: hero.MaxMana,
            MonsterId: monster.Id,
            MonsterName: monster.Name,
            MonsterHp: b.MonsterHp,
            MonsterMaxHp: monster.MaxHp,
            MonsterMana: b.MonsterMana,
            MonsterMaxMana: monster.MaxMana,
            TurnNumber: b.TurnNumber,
            IsFinished: b.IsFinished,
            HeroWon: b.IsFinished ? b.HeroWon : null,
            EquippedMoves: hero.HeroMoves
                .Where(hm => hm.IsEquipped)
                .Select(hm => hm.ToDto())
                .ToList()
        );
    }
}
