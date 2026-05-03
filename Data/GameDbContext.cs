using Microsoft.EntityFrameworkCore;
using RpgGame.Models;

namespace RpgGame.Data;

public class GameDbContext : DbContext
{
    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<Move> Moves => Set<Move>();
    public DbSet<HeroMove> HeroMoves => Set<HeroMove>();
    public DbSet<Monster> Monsters => Set<Monster>();
    public DbSet<BattleSession> BattleSessions => Set<BattleSession>();
    public DbSet<SkillWeight> SkillWeights => Set<SkillWeight>();
    public DbSet<PendingReward> PendingRewards => Set<PendingReward>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<User>().HasIndex(u => u.Username).IsUnique();
        mb.Entity<Session>().HasIndex(s => s.Token).IsUnique();

        mb.Entity<HeroMove>().HasIndex(hm => new { hm.HeroId, hm.MoveId }).IsUnique();
        mb.Entity<SkillWeight>().HasIndex(sw => new { sw.HeroId, sw.MoveId }).IsUnique();
        mb.Entity<PendingReward>().HasIndex(pr => pr.HeroId).IsUnique();

        SeedData(mb);
    }

    private static void SeedData(ModelBuilder mb)
    {
        // Move IDs 1 and 2 are reserved as default starter moves for new heroes.
        mb.Entity<Move>().HasData(
            // Physical
            new Move { Id = 1, Name = "Slash",      Description = "A standard sword strike.",                Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 35, Accuracy = 100, ManaCost = 0 },
            new Move { Id = 3, Name = "Bite",       Description = "A sharp-toothed bite.",                   Category = MoveCategory.Physical, Archetype = MoveArchetype.Assassin, Power = 30, Accuracy = 100, ManaCost = 0 },
            new Move { Id = 4, Name = "Claw Swipe", Description = "Quick swipe with razor claws.",           Category = MoveCategory.Physical, Archetype = MoveArchetype.Assassin, Power = 40, Accuracy = 95,  ManaCost = 0 },
            new Move { Id = 5, Name = "Tail Whip",  Description = "Heavy tail strike, sometimes misses.",    Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 55, Accuracy = 80,  ManaCost = 0 },
            new Move { Id = 6, Name = "Crush",      Description = "Devastating slam from a huge foe.",       Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 70, Accuracy = 75,  ManaCost = 0 },
            new Move { Id = 11, Name = "Backstab",     Description = "A sneaky strike from the shadows.",       Category = MoveCategory.Physical, Archetype = MoveArchetype.Assassin, Power = 45, Accuracy = 90,  ManaCost = 0 },
            new Move { Id = 12, Name = "Pounce",       Description = "Leap onto the target with full body.",    Category = MoveCategory.Physical, Archetype = MoveArchetype.Assassin, Power = 50, Accuracy = 85,  ManaCost = 0 },
            new Move { Id = 13, Name = "Boulder Toss", Description = "Hurls a massive rock.",                   Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 60, Accuracy = 80,  ManaCost = 0 },
            new Move { Id = 14, Name = "Howl Strike",  Description = "Battle-frenzied lunge.",                  Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 38, Accuracy = 95,  ManaCost = 0 },
            new Move { Id = 15, Name = "Tail Sweep",   Description = "Wide tail attack that's hard to dodge.",  Category = MoveCategory.Physical, Archetype = MoveArchetype.Fighter,  Power = 50, Accuracy = 90,  ManaCost = 0 },

            // Magic
            new Move { Id = 2, Name = "Magic Missile", Description = "A reliable arcane bolt.",                 Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 30, Accuracy = 100, ManaCost = 8 },
            new Move { Id = 7, Name = "Ember",         Description = "A small fireball.",                       Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 35, Accuracy = 100, ManaCost = 10 },
            new Move { Id = 8, Name = "Frost Bolt",    Description = "An icy projectile.",                      Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 45, Accuracy = 95,  ManaCost = 14 },
            new Move { Id = 9, Name = "Shadow Curse",  Description = "Dark magic that drains the target.",      Category = MoveCategory.Magic, Archetype = MoveArchetype.Assassin, Power = 55, Accuracy = 90,  ManaCost = 18 },
            new Move { Id = 10, Name = "Inferno",      Description = "Massive fire magic.",                     Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 80, Accuracy = 85,  ManaCost = 28 },
            new Move { Id = 16, Name = "Spark",        Description = "A weak but cheap zap of electricity.",    Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 25, Accuracy = 100, ManaCost = 6 },
            new Move { Id = 17, Name = "Stone Spike",  Description = "Earth magic that punctures armor.",       Category = MoveCategory.Magic, Archetype = MoveArchetype.Fighter,  Power = 50, Accuracy = 90,  ManaCost = 16 },
            new Move { Id = 18, Name = "Mind Lance",   Description = "Pierces the target's defenses with thought.", Category = MoveCategory.Magic, Archetype = MoveArchetype.Assassin, Power = 40, Accuracy = 95,  ManaCost = 12 },
            new Move { Id = 19, Name = "Frost Nova",   Description = "Burst of ice all around.",                Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 60, Accuracy = 85,  ManaCost = 22 },
            new Move { Id = 20, Name = "Soul Drain",   Description = "Painful curse that saps life.",           Category = MoveCategory.Magic, Archetype = MoveArchetype.Assassin, Power = 50, Accuracy = 95,  ManaCost = 16 },
            new Move { Id = 21, Name = "Earthen Wrath",Description = "Pillar of stone smashes the target.",     Category = MoveCategory.Magic, Archetype = MoveArchetype.Fighter,  Power = 70, Accuracy = 80,  ManaCost = 24 },
            new Move { Id = 22, Name = "Voidfire",     Description = "Black flames that defy reality.",         Category = MoveCategory.Magic, Archetype = MoveArchetype.Mage,     Power = 75, Accuracy = 88,  ManaCost = 26 }
        );

        mb.Entity<Monster>().HasData(
            new Monster { Id = 1, Name = "Goblin Scout",   RunOrder = 1, MaxHp = 60,  MaxMana = 30, Attack = 12, Defense = 8,  Speed = 10, XpReward = 60,  MoveIdsCsv = "3,11,16,18" },
            new Monster { Id = 2, Name = "Forest Wolf",    RunOrder = 2, MaxHp = 80,  MaxMana = 25, Attack = 16, Defense = 10, Speed = 14, XpReward = 90,  MoveIdsCsv = "3,4,12,14" },
            new Monster { Id = 3, Name = "Frost Mage",     RunOrder = 3, MaxHp = 90,  MaxMana = 70, Attack = 12, Defense = 10, Speed = 12, XpReward = 130, MoveIdsCsv = "2,8,19,18" },
            new Monster { Id = 4, Name = "Stone Troll",    RunOrder = 4, MaxHp = 140, MaxMana = 40, Attack = 20, Defense = 16, Speed = 7,  XpReward = 180, MoveIdsCsv = "5,6,13,17" },
            new Monster { Id = 5, Name = "Shadow Dragon",  RunOrder = 5, MaxHp = 180, MaxMana = 90, Attack = 24, Defense = 18, Speed = 16, XpReward = 260, MoveIdsCsv = "10,9,22,20" }
        );
    }
}
