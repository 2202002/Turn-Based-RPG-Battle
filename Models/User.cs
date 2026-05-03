namespace RpgGame.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Hero> Heroes { get; set; } = new();
    public List<Session> Sessions { get; set; } = new();
}
