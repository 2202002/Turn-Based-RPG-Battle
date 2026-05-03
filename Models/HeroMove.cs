namespace RpgGame.Models;

public class HeroMove
{
    public int Id { get; set; }

    public int HeroId { get; set; }
    public Hero Hero { get; set; } = null!;

    public int MoveId { get; set; }
    public Move Move { get; set; } = null!;

    public bool IsEquipped { get; set; }
    public int UpgradeLevel { get; set; } = 0;
}
