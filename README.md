# PropHunt MiniGame

[🇬🇧 English](README.md) · [🇷🇺 Русский](README.ru.md) · [🇺🇦 Українська](README.uk.md)

Configurable PropHunt minigame plugin for **SCP: Secret Laboratory** servers using **EXILED 9.14.2+**.

> **GitHub description:** A configurable SCP: Secret Laboratory PropHunt minigame with EXILED NPCs, solo testing, smooth bot behaviour, multilingual messages, runtime settings, and hider-versus-hunter win conditions.

## Features

- One random hunter versus real-player hiders disguised among NPCs.
- `test_prophunt` with an exact NPC count, including one-player servers.
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
| `prophunt` | Start a regular game; requires at least two alive non-NPC players |
| `prophunt start` | Same as `prophunt` |
| `prophunt stop` | Stop the game, remove NPCs, unlock the arena, and clean up |
| `prophunt settings` | Show current runtime settings |
| `prophunt settings <name> <value>` | Change a setting for the current plugin session |
| `test_prophunt <npc_count>` | Start a test with exactly the requested NPC count |

Aliases: `startPropHunt`, `ttstart`, `testprophunt`.

```text
prophunt
prophunt stop
prophunt settings language english
prophunt settings dummies 5
prophunt settings penaltypercent 30
test_prophunt 19
```

`test_prophunt 19` works with one alive player and is limited by `maxdummies`.

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
| `radius` | Arena wandering radius | `28` |
| `debug` | Enable debug logging | `false` |

`IsEnabled`, `UniformNickname`, and `UniformCustomInfo` are configured in the generated EXILED config file. Console changes are runtime-only unless copied to that file.

## Rules and damage

Regular NPC count is `hiders × dummies`, capped by `maxdummies`. Shooting a real hider eliminates them. Shooting an NPC is cancelled and damages the hunter:

```text
actual penalty = penaltypercent / √(number of alive NPCs)
```

When every hunter is eliminated, hiders win. When every hider is eliminated or the timer expires, the game ends.

## GitHub Releases

Each release should contain these assets:

| Asset | Purpose |
|---|---|
| `PropHuntMiniGame.dll` | Ready-to-install compiled plugin; copy to `EXILED/Plugins` |
| `PropHuntMiniGame-Source.zip` | Uncompiled open-source project for developers |

The source archive should include the `.cs` files, `.csproj`, `README*`, and project configuration, but does not need `bin/` or `obj/` build output.

## Build from source

Requirements: .NET SDK with .NET Framework 4.8 targeting support.

```powershell
dotnet restore .\PropHuntMiniGame.csproj
dotnet build .\PropHuntMiniGame.csproj
```

Output: `bin/Debug/PropHuntMiniGame.dll`.

## License

Add the repository's chosen license here when one is selected.
