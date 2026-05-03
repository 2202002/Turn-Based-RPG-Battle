namespace RpgGame.Models;

public class BattleSession
{
    public int Id { get; set; }

    public int HeroId { get; set; }
    public Hero Hero { get; set; } = null!;

    public int MonsterId { get; set; }
    public Monster Monster { get; set; } = null!;

    public int HeroHp { get; set; }
    public int HeroMana { get; set; }
    public int MonsterHp { get; set; }
    public int MonsterMana { get; set; }

    public int TurnNumber { get; set; } = 1;
    public bool IsFinished { get; set; } = false;
    public bool HeroWon { get; set; } = false;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
