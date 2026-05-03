# Nordeus Full Stack Challenge - Backend

My submission for the Nordeus Full Stack Challenge 2026. I went backend-only since I had about a day and a half to do it.

Stack: .NET 8 Web API, EF Core, SQLite, BCrypt for password hashing.

## How to run

You need .NET 8 SDK installed.

```
cd RpgGame
dotnet restore
dotnet run
```

Then open http://localhost:5050/swagger and you can play around with the API there.

The SQLite database (`rpggame.db`) is created automatically on first run with all the seed data (moves and monsters). If you want a clean state, just delete the file and run again.

## The game

You make an account, create a hero, and fight 5 monsters in order. Each fight is turn-based - you pick a move, the monster picks one back, repeat until somebody hits 0 HP. After you win, you get to pick 1 of 3 random skills from the monster's pool. You can replay any monster as many times as you want to grind XP or try to get a different skill.

One account can have multiple heroes. Each hero has its own progression.

### Moves

Each move is either physical or magic.
- Physical uses your Attack stat, no mana cost.
- Magic ignores your Attack (it scales with the spell's own power), but costs mana. Magic hits a bit harder to make up for it.

Heroes start with two moves equipped: Slash (physical) and Magic Missile (magic). You can equip up to 4 at a time. Total moves you can know is capped at 10. If you don't want a move anymore you can drop it (but not if it's currently equipped).

### Reward picks

When you kill a monster, you get to choose 1 out of 3 skills from its 4-skill pool. The 3 are picked weighted-randomly:
- Each (hero, move) pair has a "weight" that starts at 100.
- Every time a move is shown to you, its weight gets multiplied by 0.7 (so it shows up a bit less next time).
- Floor is 5 so it never disappears completely.
- If you drop a move, its weight gets cut in half on top of that.

So basically the more you've seen a skill, the rarer it gets. This way you keep seeing different stuff instead of the same 3 picks every time.

If you pick a skill you don't have yet, you learn it at level 0. If you pick one you already know, you upgrade it by 1 (max 5).

### Upgrade tiers and archetypes

Every move has an archetype (Fighter, Mage, or Assassin). It only matters at MAX (level 5).

Levels 1-4 are basically just stat bumps - more power, less mana cost, slightly better accuracy. Mana goes to 0 at level 5.

Level 5 (MAX) gives the move its archetype's special passive:
- Fighter MAX = lifesteal, you heal for 30% of damage you deal with this move
- Mage MAX = guaranteed crit, every hit does 2x damage
- Assassin MAX = execute, 2.5x damage if the monster has less than 40% HP

So the moves you choose to MAX kind of define your build. Hero classes don't exist - your loadout makes you into something.

## API endpoints

All routes except `/api/auth/*` need an `Authorization: Bearer <token>` header. You get the token from register or login.

```
POST   /api/auth/register                          { username, password }
POST   /api/auth/login                             { username, password }

GET    /api/heroes                                 list my heroes
POST   /api/heroes                                 { name }   create a hero
GET    /api/heroes/{id}                            hero detail
PUT    /api/heroes/{id}/moves                      { equippedHeroMoveIds: [..] }
DELETE /api/heroes/{id}/moves/{heroMoveId}         drop a move
POST   /api/heroes/{id}/run/restart                reset run progress

POST   /api/heroes/{id}/battle/start               { monsterId? }
GET    /api/heroes/{id}/battle/active              get active battle state
POST   /api/heroes/{id}/battle/action              { heroMoveId }   one turn

GET    /api/heroes/{id}/reward/pending             re-fetch the 3-skill picks
POST   /api/heroes/{id}/reward/choose              { moveId }   learn or upgrade

GET    /api/monsters                               monster catalog
```

The `battle/action` response has the updated state plus an event log (`move_used`, `miss`, `damage`, `crit`, `execute`, `lifesteal`, `victory`, `defeat`, `level_up`, `run_completed`). When you win, the response also includes a `pendingReward` with the 3 picks.

If `battle/start` is called with no body or with `{ monsterId: null }`, the server picks the next monster in the run for you. Otherwise it uses whatever monsterId you sent.

### Sample flow

```
POST /api/auth/register   { "username": "luka", "password": "luka1234" }
   -> { "token": "...", ... }

POST /api/heroes          { "name": "Aldor" }
POST /api/heroes/1/battle/start   {}
POST /api/heroes/1/battle/action  { "heroMoveId": 1 }
... do this until the monster dies ...
POST /api/heroes/1/reward/choose  { "moveId": 11 }
PUT  /api/heroes/1/moves          { "equippedHeroMoveIds": [1, 3, 5, 7] }
POST /api/heroes/1/battle/start   {}
... etc ...
```

## Project structure

```
RpgGame/
  Controllers/        thin HTTP layer, auth via [RequireAuth] attribute
  Data/               EF Core DbContext + seed data
  Dtos/               request/response shapes
  Models/             entity classes (one per table)
  Services/           game logic
  ExceptionMiddleware maps exceptions to HTTP status codes
  Program.cs          DI setup, middleware pipeline
```

Services:
- `AuthService` - register, login, token validation
- `HeroService` - create/get heroes, equip, drop, level up logic
- `BattleService` - turn resolution, damage formula, monster AI
- `RewardService` - the 3-pick screen, weighted random sampling, choose logic
- `StatsCalculator` - hero stat formula (per-level)
- `MoveStats` - move upgrade math (per-level + archetype passives)
- `Mappers` - entity to DTO conversions

## Some notes on decisions

**Why store battle state in the DB?** Each turn is one HTTP request, so the server has to remember what's going on between requests. Putting it in memory would mean losing all battles on a restart, plus it doesn't scale to multiple instances. The DB way means you can also reload the page mid-fight and it still works.

**Why a separate "pending reward" entity?** When you win, the server can't just immediately give you a random skill - you need to pick from 3. The picks have to be saved somewhere between the action response (where they're sent) and the choose request (where the player picks). So I made a `PendingReward` table with one row per hero (max).

**Why CSV for monster moves instead of a join table?** Monster move pools never change at runtime, it's pure seed data. A join table would be 4x more rows for no real benefit. The Mapper has a `ParseMoveIds` helper that's maybe 5 lines.

**Session tokens instead of JWT.** I went with a `Sessions` table where each row is a token + expiry. Simpler than JWT and easy to invalidate (just delete the row). For a project this size it felt like overkill to do JWT.

**Exception middleware for error responses.** Services throw normal exceptions like `ArgumentException` or `KeyNotFoundException`, and the middleware maps them to 400/404/etc. Means the services don't need to know about HTTP at all.

**`EnsureCreated` instead of EF migrations.** Easier for someone reviewing the project - they just `dotnet run` and it works. For a real production app you'd want migrations but here the schema is small enough.

## Stat numbers (in case it's useful)

Hero level 1 starts with: 100 HP, 30 mana, 12 atk, 8 def, 10 spd.
Per level: +12 HP, +4 mana, +3 atk, +2 def, +1 spd.

XP needed for next level = current level * 100. So 1->2 needs 100 XP, 2->3 needs 200, etc.

Monsters scale up - Goblin Scout has 60 HP and gives 60 XP, Shadow Dragon has 180 HP and gives 260 XP.

Damage formula:
- Physical: `(Attack * Power / 10) - (Defense / 2)`
- Magic: `(Power * 1.3) - (Defense / 3)`
- Final result times a 0.85-1.0 random factor, minimum 1.

## Things I didn't do

- No frontend, just the backend.
- No JWT, no rate limiting.
- No status effects (poison, stun) or type effectiveness (fire vs grass etc).
- No automated tests. Services are small and could be unit tested if I had more time, especially the damage formula and the weighted sampling in RewardService.
