using RpgGame.Models;

namespace RpgGame.Services;

public static class StatsCalculator
{
    public const int BaseMaxHp = 100;
    public const int BaseMaxMana = 30;
    public const int BaseAttack = 12;
    public const int BaseDefense = 8;
    public const int BaseSpeed = 10;

    public const int HpPerLevel = 12;
    public const int ManaPerLevel = 4;
    public const int AttackPerLevel = 3;
    public const int DefensePerLevel = 2;
    public const int SpeedPerLevel = 1;

    public static void ApplyStatsForLevel(Hero hero)
    {
        hero.MaxHp   = BaseMaxHp   + (hero.Level - 1) * HpPerLevel;
        hero.MaxMana = BaseMaxMana + (hero.Level - 1) * ManaPerLevel;
        hero.Attack  = BaseAttack  + (hero.Level - 1) * AttackPerLevel;
        hero.Defense = BaseDefense + (hero.Level - 1) * DefensePerLevel;
        hero.Speed   = BaseSpeed   + (hero.Level - 1) * SpeedPerLevel;
    }

    public static int XpToNextLevel(int currentLevel) => currentLevel * 100;
}
