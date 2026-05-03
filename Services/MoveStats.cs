using RpgGame.Models;

namespace RpgGame.Services;

public static class MoveStats
{
    public const int MaxUpgradeLevel = 5;

    public const double MageCritMultiplier = 2.0;
    public const double FighterLifestealFraction = 0.30;
    public const double AssassinExecuteMultiplier = 2.5;
    public const double AssassinExecuteHpThreshold = 0.40;

    public record EffectiveMove(
        int Power,
        int Accuracy,
        int ManaCost,
        string DisplayName,
        bool MageGuaranteedCrit,
        bool FighterLifesteal,
        bool AssassinExecute,
        string? MaxPassiveLabel
    );

    public static EffectiveMove Compute(Move baseMove, int upgradeLevel)
    {
        if (upgradeLevel < 0) upgradeLevel = 0;
        if (upgradeLevel > MaxUpgradeLevel) upgradeLevel = MaxUpgradeLevel;

        int power = baseMove.Power;
        int accuracy = baseMove.Accuracy;
        int mana = baseMove.ManaCost;

        switch (upgradeLevel)
        {
            case 1:
                power += 4;
                mana = ReduceMana(mana, 0.15);
                break;
            case 2:
                power += 8;
                accuracy = Math.Min(100, accuracy + 1);
                mana = ReduceMana(mana, 0.30);
                break;
            case 3:
                power += 12;
                accuracy = Math.Min(100, accuracy + 2);
                mana = ReduceMana(mana, 0.45);
                break;
            case 4:
                power += 16;
                accuracy = Math.Min(100, accuracy + 3);
                mana = ReduceMana(mana, 0.60);
                break;
            case 5:
                power += 20;
                accuracy = Math.Min(100, accuracy + 4);
                mana = 0;
                break;
        }

        bool isMax = upgradeLevel == MaxUpgradeLevel;
        bool mageCrit  = isMax && baseMove.Archetype == MoveArchetype.Mage;
        bool fighterLs = isMax && baseMove.Archetype == MoveArchetype.Fighter;
        bool assassin  = isMax && baseMove.Archetype == MoveArchetype.Assassin;

        string? label = !isMax ? null : baseMove.Archetype switch
        {
            MoveArchetype.Mage     => $"Guaranteed Crit ({MageCritMultiplier}x)",
            MoveArchetype.Fighter  => $"Lifesteal ({(int)(FighterLifestealFraction * 100)}%)",
            MoveArchetype.Assassin => $"Execute (<{(int)(AssassinExecuteHpThreshold * 100)}% HP -> {AssassinExecuteMultiplier}x)",
            _ => null
        };

        return new EffectiveMove(
            Power: power,
            Accuracy: accuracy,
            ManaCost: mana,
            DisplayName: BuildDisplayName(baseMove.Name, upgradeLevel),
            MageGuaranteedCrit: mageCrit,
            FighterLifesteal: fighterLs,
            AssassinExecute: assassin,
            MaxPassiveLabel: label
        );
    }

    public static string BuildDisplayName(string baseName, int upgradeLevel) => upgradeLevel switch
    {
        0 => baseName,
        1 => baseName + "+",
        2 => baseName + "++",
        3 => baseName + "+++",
        4 => baseName + "++++",
        5 => baseName + " MAX",
        _ => baseName
    };

    private static int ReduceMana(int currentMana, double reductionFraction)
    {
        if (currentMana <= 0) return 0;
        int reduced = (int)Math.Floor(currentMana * (1.0 - reductionFraction));
        return Math.Max(1, reduced);
    }
}
