# PropHunt MiniGame

[🇬🇧 English](README.md) · [🇷🇺 Русский](README.ru.md) · [🇺🇦 Українська](README.uk.md)

Configurable PropHunt minigame plugin for **SCP: Secret Laboratory** servers using **EXILED 9.14.2+**.

> **GitHub description:** A configurable SCP: Secret Laboratory PropHunt minigame with EXILED NPCs, solo testing, smooth bot behaviour, multilingual messages, runtime settings, and hider-versus-hunter win conditions.

## Plugin Information

- **Game:** SCP: Secret Laboratory
- **Plugin version:** 0.2.1
- **EXILED:** 9.14.2+
- **Source code:** [github.com/l0mive/PropHunt-EXILED-Plugin](https://github.com/l0mive/PropHunt-EXILED-Plugin)
- **Latest download:** [GitHub Releases](https://github.com/l0mive/PropHunt-EXILED-Plugin/releases/latest)
- **Dependencies:** EXILED 9.14.2+ and .NET Framework 4.8

Server owners are responsible for checking that their use of this plugin complies with the [Community Server Guidelines](https://scpslgame.com/CSG.pdf).

## Features

- One random hunter versus real-player hiders disguised among NPCs.
- `test_prophunt` with an exact NPC count, including one-player servers.
- Arena selection at game start: `049`, `surface_gate`, or `here`.
- Persistent runtime settings saved directly to the EXILED server configuration.
- Smooth NPC turning, acceleration, wandering, following, pauses, obstacle avoidance, separation, and random jumps.
- NPC-hit damage is a percentage of hunter max HP and decreases as the NPC count increases.
- Hunters move to Spectator when their HP reaches zero; hiders win when all hunters are eliminated.
- Arena locking, round timer, cleanup on round end, and safe NPC spawn retries.
- English, Russian, and Ukrainian messages.

## Installation

1. Download `PropHuntMiniGame.dll` from the GitHub Release assets.
2. Copy it to the server's `EXILED/Plugins` directory.
3. Run EXILED 9.14.2 or a compatible newer release.
4. Restart the server. EXILED creates the configuration automatically.

The source ZIP is optional and is intended for developers who want to inspect or build the plugin themselves.

## Permission

Both commands require:

```text
PropHunt.start
```

Commands are registered for Remote Admin and the game console.

## Commands

| Command | Description |
|---|---|
| `prophunt` | Show command usage |
| `prophunt start <arena>` | Start a regular game in `049`, `surface_gate`, or `here`; requires at least two alive non-NPC players |
| `prophunt stop` | Stop the game, remove NPCs, unlock the arena, and clean up |
| `prophunt settings` | Show current runtime settings |
| `prophunt settings <name> <value>` | Change and save a setting to the server configuration |
| `test_prophunt <npc_count> <arena>` | Start a test with exactly the requested NPC count in `049`, `surface_gate`, or `here` |

Aliases: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt start 049
prophunt stop
prophunt settings language english
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19 surface_gate
```

`test_prophunt 19 surface_gate` works with one alive player and is limited by `maxdummies`.

## Runtime settings

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
| `radius` | Arena wandering radius | `18` |
| `debug` | Enable debug logging | `false` |

Select the arena only when starting a game: `prophunt start <arena>` or `test_prophunt <npc_count> <arena>`. The `049` preset uses a validated floor position beside the SCP-049 Armory door, away from the containment-gate collider; every participant and NPC position is checked for floor clearance and the correct floor level before teleporting. Its containment doors, including the 049 and new 173 gates, are opened and locked; the 049 lift stays closed and locked. The `surface_gate` preset starts on the surface side of the Surface Gate. Surface doors are opened and locked, while Gate A and Gate B lifts remain locked. Arena doors are prepared one second before any player or NPC is teleported. The `here` option uses the position of the alive player who starts the game.

`IsEnabled`, `UniformNickname`, and `UniformCustomInfo` are configured in the generated EXILED config file. Every successful `prophunt settings <name> <value>` change is also saved there and remains after a server restart. The `here` arena must be started by an alive in-game player, not the server console.

## Rules and damage

Regular NPC count is `hiders × dummies`, capped by `maxdummies`. Shooting a real hider eliminates them. Shooting an NPC is cancelled and damages the hunter:

```text
actual penalty = penaltypercent / √(number of alive NPCs)
```

When every hunter is eliminated, hiders win. When every hider is eliminated or the timer expires, the game ends.

## Downloads

Download **`PropHuntMiniGame.dll`** from the latest Release and copy it to your server's `EXILED/Plugins` directory.

**`PropHuntMiniGame-Source.zip`** contains the uncompiled open-source project for developers who want to inspect or build the plugin.

## Build from source

Requirements: .NET SDK with .NET Framework 4.8 targeting support.

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

Output: `bin/Debug/PropHuntMiniGame.dll`.

## License

Add the repository's chosen license here when one is selected.
