namespace RpgGame.Models;

public class Monster
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int RunOrder { get; set; }

    public int MaxHp { get; set; }
    public int MaxMana { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    public int XpReward { get; set; }

    // CSV of Move.Id values - parsed at runtime via Mappers.ParseMoveIds.
    public string MoveIdsCsv { get; set; } = "";
}
