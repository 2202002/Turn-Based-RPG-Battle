using Microsoft.EntityFrameworkCore;
using RpgGame.Data;
using RpgGame.Dtos;
using RpgGame.Models;

namespace RpgGame.Services;

public class AuthService
{
    private readonly GameDbContext _db;
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

    public AuthService(GameDbContext db) => _db = db;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters.");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.");

        var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
        if (exists) throw new InvalidOperationException("Username already taken.");

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await CreateSessionAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return await CreateSessionAsync(user);
    }

    public async Task<User?> GetUserByTokenAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var session = await _db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Token == token);

        if (session == null) return null;
        if (session.ExpiresAt < DateTime.UtcNow) return null;
        return session.User;
    }

    private async Task<AuthResponse> CreateSessionAsync(User user)
    {
        var session = new Session
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.Add(SessionLifetime)
        };
        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return new AuthResponse(session.Token, user.Username, session.ExpiresAt);
    }
}
