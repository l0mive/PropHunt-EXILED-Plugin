---

## English

### What it does

PropHunt hides real players among NPCs. One random player becomes the hunter; the remaining players become hiders and are disguised to look like NPCs. The hunter must find the hiders without wasting health on NPCs.

Features:

- Regular games with one hunter and multiple hiders.
- Exact-count NPC tests, including a one-player server.
- Smooth NPC turning, acceleration, wandering, following, pauses, obstacle avoidance, separation, and random jumps.
- Percentage-based NPC-hit damage that decreases as the NPC count increases.
- Automatic spectator elimination for hunters and hider victory when all hunters are gone.
- Arena locking, round timer, cleanup on round end, and safe NPC spawn retries.
- English, Russian, and Ukrainian messages.

### Installation

Copy `PropHuntMiniGame.dll` to `EXILED/Plugins` and restart the server. EXILED 9.14.2 or a compatible newer release is required. EXILED creates the configuration automatically.

### Permission

Both commands require:

```text
PropHunt.start
```

Commands are registered for Remote Admin and the game console.

### Commands

| Command | Description |
|---|---|
| `prophunt` | Start a regular game; requires at least two alive non-NPC players |
| `prophunt start` | Same as `prophunt` |
| `prophunt stop` | Stop the game, remove NPCs, unlock the arena, and clean up |
| `prophunt settings` | Show current runtime settings |
| `prophunt settings <name> <value>` | Change a setting for the current plugin session |
| `test_prophunt <npc_count>` | Start a test with exactly the requested NPC count |

Aliases: `startPropHunt`, `ttstart`, `testprophunt`.

Examples:

```text
prophunt
prophunt stop
prophunt settings language english
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19
```

`test_prophunt 19` works with one alive player and is limited by `maxdummies`.

### Runtime settings

| Name | Description | Default |
|---|---|---:|
| `language` / `lang` | `english`, `russian`, or `ukrainian` | `russian` |
| `dummies` | NPCs per hider in a regular game | `3` |
| `maxdummies` | Maximum NPCs per game/test | `60` |
| `spawnattempts` | NPC spawn retry count | `4` |
| `penalty` / `penaltypercent` | Base hunter max-HP percentage lost for hitting an NPC | `25` |
| `duration` | Round duration in seconds | `180` |
| `followchance` | Chance to follow a nearby player (`0`–`1`) | `0.18` |
| `walkspeed` / `followspeed` | NPC movement speeds | `1.75` / `2.25` |
| `turnspeed` | Maximum turn speed in degrees/second | `75` |
| `pausechance` | Chance of a short pause (`0`–`1`) | `0.30` |
| `pausemin` / `pausemax` | Random pause duration range | `0.45` / `1.35` |
| `jumpchance` | Chance to jump when changing behaviour (`0`–`1`) | `0.12` |
| `radius` | Arena wandering radius | `28` |
| `debug` | Enable debug logging | `false` |

Console settings are runtime-only unless copied to the generated EXILED configuration file.

Configuration-file-only options are `IsEnabled`, `UniformNickname` (the name shown for participants/NPCs), and `UniformCustomInfo` (the displayed custom info/prefix).

### Rules and damage

In a regular game, NPC count is `hiders × dummies`, capped by `maxdummies`. Shooting a real hider eliminates that player. Shooting an NPC is cancelled and damages the hunter using:

```text
actual penalty = penaltypercent / √(number of alive NPCs)
```

When a hunter reaches zero HP, they are moved to Spectator. If every hunter is eliminated, hiders win. If every hider is eliminated or the timer expires, the game ends.

### Build

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

Output: `bin/Debug/PropHuntMiniGame.dll`.

---
