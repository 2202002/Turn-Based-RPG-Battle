namespace RpgGame.Models;

public class PendingReward
{
    public int Id { get; set; }

    public int HeroId { get; set; }
    public Hero Hero { get; set; } = null!;

    public int MonsterId { get; set; }
    public Monster Monster { get; set; } = null!;

    public string ChoiceMoveIdsCsv { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
