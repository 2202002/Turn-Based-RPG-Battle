namespace RpgGame.Models;

public class SkillWeight
{
    public const double InitialWeight = 100.0;
    public const double DecayMultiplier = 0.70;
    public const double MinWeight = 5.0;

    public int Id { get; set; }

    public int HeroId { get; set; }
    public Hero Hero { get; set; } = null!;

    public int MoveId { get; set; }
    public Move Move { get; set; } = null!;

    public double Weight { get; set; } = InitialWeight;
    public int TimesOffered { get; set; } = 0;
}
