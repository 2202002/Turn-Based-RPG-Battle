namespace RpgGame.Models;

public enum MoveCategory
{
    Physical = 0,
    Magic = 1
}

public enum MoveArchetype
{
    Fighter = 0,
    Mage = 1,
    Assassin = 2
}

public class Move
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public MoveCategory Category { get; set; }
    public MoveArchetype Archetype { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public int ManaCost { get; set; }
}
