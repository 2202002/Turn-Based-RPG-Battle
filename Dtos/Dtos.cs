using RpgGame.Models;

namespace RpgGame.Dtos;

// ===== Auth =====

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);
public record AuthResponse(string Token, string Username, DateTime ExpiresAt);

// ===== Hero =====

public record CreateHeroRequest(string Name);

public record HeroDto(
    int Id,
    string Name,
    int Level,
    int Xp,
    int XpToNextLevel,
    int MaxHp,
    int MaxMana,
    int Attack,
    int Defense,
    int Speed,
    int RunProgress,
    bool RunCompleted,
    int MaxLearnedMoves,
    int MaxEquippedMoves,
    List<HeroMoveDto> Moves
);

public record HeroMoveDto(
    int Id,
    int MoveId,
    string Name,
    string DisplayName,
    string Description,
    string Category,
    string Archetype,
    int Power,
    int Accuracy,
    int ManaCost,
    int UpgradeLevel,
    int MaxUpgradeLevel,
    string? MaxPassiveLabel,
    bool IsEquipped
);

public record EquipMovesRequest(List<int> EquippedHeroMoveIds);

// ===== Monsters =====

public record MonsterDto(
    int Id,
    string Name,
    int RunOrder,
    int MaxHp,
    int MaxMana,
    int Attack,
    int Defense,
    int Speed,
    List<MoveDto> Moves
);

public record MoveDto(
    int Id,
    string Name,
    string Description,
    string Category,
    string Archetype,
    int Power,
    int Accuracy,
    int ManaCost
);

// ===== Battle =====

public record StartBattleRequest(int? MonsterId);

public record BattleEventDto(
    string Actor,
    string Type,
    string Message,
    int? Damage = null,
    int? ManaSpent = null,
    int? HeroHpAfter = null,
    int? MonsterHpAfter = null,
    int? HeroManaAfter = null,
    int? MonsterManaAfter = null,
    bool IsCrit = false
);

public record BattleStateDto(
    int BattleId,
    int HeroId,
    string HeroName,
    int HeroHp,
    int HeroMaxHp,
    int HeroMana,
    int HeroMaxMana,
    int MonsterId,
    string MonsterName,
    int MonsterHp,
    int MonsterMaxHp,
    int MonsterMana,
    int MonsterMaxMana,
    int TurnNumber,
    bool IsFinished,
    bool? HeroWon,
    List<HeroMoveDto> EquippedMoves
);

public record BattleActionRequest(int HeroMoveId);

// ===== Rewards =====

public record RewardChoiceDto(
    int MoveId,
    string Name,
    string Description,
    string Category,
    string Archetype,
    int BasePower,
    int BaseAccuracy,
    int BaseManaCost,
    bool AlreadyKnown,
    int? CurrentUpgradeLevel,
    int? NextUpgradeLevel,
    bool IsAtMax,
    double Weight
);

public record PendingRewardDto(
    int MonsterId,
    string MonsterName,
    List<RewardChoiceDto> Choices
);

public record ChooseRewardRequest(int MoveId);

public record ChooseRewardResponse(
    HeroDto Hero,
    MoveDto? LearnedMove,
    HeroMoveDto? UpgradedMove,
    bool MaxedOut
);

public record BattleActionResponse(
    BattleStateDto State,
    List<BattleEventDto> Events,
    HeroDto? UpdatedHero,
    PendingRewardDto? PendingReward
);
