namespace RpgGame.Models;

public class Hero
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = "";

    public int Level { get; set; } = 1;
    public int Xp { get; set; } = 0;

    public int MaxHp { get; set; }
    public int MaxMana { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }

    public int RunProgress { get; set; } = 0;
    public bool RunCompleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<HeroMove> HeroMoves { get; set; } = new();
}
