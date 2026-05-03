using RpgGame.Dtos;
using RpgGame.Models;

namespace RpgGame.Services;

public static class Mappers
{
    public static MoveDto ToDto(this Move m) => new(
        m.Id, m.Name, m.Description, m.Category.ToString(), m.Archetype.ToString(),
        m.Power, m.Accuracy, m.ManaCost
    );

    public static HeroMoveDto ToDto(this HeroMove hm)
    {
        var eff = MoveStats.Compute(hm.Move, hm.UpgradeLevel);
        return new HeroMoveDto(
            Id: hm.Id,
            MoveId: hm.MoveId,
            Name: hm.Move.Name,
            DisplayName: eff.DisplayName,
            Description: hm.Move.Description,
            Category: hm.Move.Category.ToString(),
            Archetype: hm.Move.Archetype.ToString(),
            Power: eff.Power,
            Accuracy: eff.Accuracy,
            ManaCost: eff.ManaCost,
            UpgradeLevel: hm.UpgradeLevel,
            MaxUpgradeLevel: MoveStats.MaxUpgradeLevel,
            MaxPassiveLabel: eff.MaxPassiveLabel,
            IsEquipped: hm.IsEquipped
        );
    }

    public static HeroDto ToDto(this Hero h) => new(
        h.Id,
        h.Name,
        h.Level,
        h.Xp,
        StatsCalculator.XpToNextLevel(h.Level),
        h.MaxHp,
        h.MaxMana,
        h.Attack,
        h.Defense,
        h.Speed,
        h.RunProgress,
        h.RunCompleted,
        HeroService.MaxLearnedMoves,
        HeroService.MaxEquippedMoves,
        h.HeroMoves.Select(hm => hm.ToDto()).ToList()
    );

    public static MonsterDto ToDto(this Monster monster, IEnumerable<Move> allMoves)
    {
        var moveIds = ParseMoveIds(monster.MoveIdsCsv);
        var moves = allMoves.Where(m => moveIds.Contains(m.Id)).Select(m => m.ToDto()).ToList();
        return new MonsterDto(
            monster.Id, monster.Name, monster.RunOrder,
            monster.MaxHp, monster.MaxMana,
            monster.Attack, monster.Defense, monster.Speed,
            moves
        );
    }

    public static List<int> ParseMoveIds(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<int>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Select(int.Parse)
                 .ToList();
}
