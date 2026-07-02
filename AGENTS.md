# AGENTS.md

## Project Overview

ManosabaLin is a Slay the Spire 2 gameplay/content mod built with C#/.NET and Godot. Unlike MinionLib, this repository is not a reusable library; it is a player-facing content mod.

The mod adds and wires together:

- Custom characters, including Ema/Emalin, Hiro, and Sherrylin content.
- Cards, card pools, relics, potions, powers, enchantments, afflictions, orbs, events, monsters, encounters, and run-history assets.
- Godot scenes and images used by the mod PCK.
- FMOD audio banks and card/character sound helpers.
- Harmony patches, console commands, optional compatibility gates, and telemetry setup.

Important paths:

- `ManosabaLinCode/`: C# mod code.
- `ManosabaLinCode/MainFile.cs`: mod initializer, RitsuLib content-pack setup, MinionLib/RitsuLib discovery, FMOD bank loading, Harmony patching, and custom pile registration.
- `ManosabaLinCode/Characters/Common/`: shared templates and common helpers for cards, characters, powers, relics, potions, actions, components, keywords, and targeting behavior.
- `ManosabaLinCode/Characters/{Ema,Hiro,Sherrylin}/`: character-specific cards, relics, powers, components, events, monsters, orbs, and other gameplay code.
- `ManosabaLinCode/Compat/`: optional mod compatibility helpers.
- `ManosabaLin/`: Godot assets packed into the mod PCK.
- `ManosabaLin/localization/{eng,zhs,jpn}/`: localization JSON files. Keep supported languages structurally aligned when adding public text.
- `ManosabaLin/images/`: card, relic, power, UI, map, and run-history art.
- `ManosabaLin/scenes/`: Godot scenes for characters, monsters, events, orbs, UI, and backgrounds.
- `FMOD/`: FMOD Studio project, scripts, metadata, source assets, and generated build output.

## Local Context File

Always read `AGENTS.local.md` after this file when working in this repository.

`AGENTS.local.md` stores machine-specific paths and must not be committed. In this repository it must include all three required external context paths:

- Slay the Spire 2 source/decompiled project directory.
- MinionLib source directory.
- STS2-RitsuLib source directory.

These paths are required because this content mod depends on the base game, MinionLib, and RitsuLib. Use them for API checks before guessing signatures, resource shapes, registration behavior, or compatibility details.

Do not commit personal path files such as `AGENTS.local.md` or `local.props`.

If `AGENTS.local.md` is missing, or if a required path is missing or points to a non-existent directory, use the repository skill `.codex/skills/manosabalin-local-context` to create or repair it. The skill searches common local project roots and should ask the user only for required paths it cannot find.

## Environment

This repository targets:

- .NET `net9.0`.
- Godot.NET.Sdk `4.5.1`.
- C# `preview`.
- Godot/MegaDot 4.5.1-compatible exports.
- NuGet dependency `STS2.RitsuLib`.
- NuGet dependency `FuYnAloft.Sts2.MinionLib`.

Dependency version rule: do not proactively update dependency versions during normal work. Use the versions currently declared by the project files. If the user has already updated a dependency version, treat that updated declaration as the source of truth and work with it.

Local game/tool paths are usually configured through `Directory.Build.props` and `local.props`.

Useful MSBuild properties:

- `GodotPath`: path to the MegaDot/Godot executable.
- `SteamLibraryPath`: Steam `steamapps` directory.
- `Sts2Path`: Slay the Spire 2 install directory.
- `ModsPath`: Slay the Spire 2 `mods/` directory.
- `Sts2DataDir`: directory containing `sts2.dll`, `0Harmony.dll`, and `Steamworks.NET.dll`.
- `FmodStudioPath`: path to `fmodstudiocl.exe`.
- `RitsuLibDeployDir`: RitsuLib deployment directory under the game's `mods/` folder.

## Common Commands

Use PowerShell on Windows unless the user asks otherwise.

- Build solution: `dotnet build ManosabaLin.sln`
- Build without PCK export in the current shell: `$env:STS2_SKIP_PCK_EXPORT='1'; dotnet build ManosabaLin.sln`
- Publish/export mod assets when local Godot and FMOD paths are valid: `dotnet publish ManosabaLin.csproj`

Builds require valid STS2 reference DLL paths. Publish/export may also require a valid `GodotPath`, `FmodStudioPath`, and game install path. If a command fails because local paths are missing, inspect `AGENTS.local.md`, `Directory.Build.props`, and `local.props` before changing code.

If dependency restore fails because of permissions, sandboxing, or an unexpected NuGet global-packages location, try pointing NuGet at the current Windows user's package cache before changing project files:

```powershell
$env:NUGET_PACKAGES = Join-Path $env:USERPROFILE '.nuget\packages'
dotnet build ManosabaLin.sln
```

Use `dotnet nuget locals global-packages --list` and `Test-Path $env:NUGET_PACKAGES` to confirm which cache the build is using.

## Coding Notes

- Prefer `rg` / `rg --files` for search.
- Use `apply_patch` for hand edits.
- Do not edit generated files such as `*.g.cs`, generated FMOD output, or Godot `*.uid` sidecar files unless the user explicitly asks.
- Do not revert user changes in the working tree.
- Keep changes scoped. This is a large content mod, so avoid broad style cleanup while fixing one card, relic, scene, or localization entry.
- Prefer existing shared templates in `ManosabaLinCode/Characters/Common/` over direct base-game/RitsuLib inheritance in new content.
- Use `MainFile.ModId`, `MainFile.Slug`, `MainFile.ResPath`, and resource path helpers instead of hard-coded duplicate IDs where practical.
- Cards normally derive from `ManosabaCardTemplate` or a character-specific specialization. Card art lookup is based on the class name lowercased, first under `ManosabaLin/images/cards/big/`, then the smaller card image path, then a fallback.
- Character select sounds are derived from the character class name in `ManosabaCharacterTemplate`.
- RitsuLib auto-registration attributes and scaffolding templates are the preferred path for normal content registration.
- MinionLib is used for component-card behavior and component-related interfaces. Check the MinionLib source when touching card components, component serialization, dynamic vars, or right-click/timing hooks.
- Harmony patches should stay narrow, documented by surrounding code, and checked against the decompiled base-game source before changing method signatures or control flow assumptions.
- For localization changes, update every relevant locale file and keep key names consistent with registered content IDs.
- For Godot resources, keep `.tscn`, `.import`, image paths, and code-side `res://ManosabaLin/...` references in sync.
- For FMOD changes, keep the FMOD project, generated GUID mappings, and runtime event paths aligned with `ManosabaLinCode/Audio/` helpers.
- Put agent-directed temporary outputs such as copied mod DLLs, ad hoc diagnostic logs, and throwaway build/export output under `.local/` or one of its subdirectories. Tool-managed project caches and standard build folders such as `.godot/`, `bin/`, and `obj/` do not need to be redirected.

## When To Use External Repositories

Use `AGENTS.local.md` paths for external context:

- Use the Slay the Spire 2 source/decompiled project when checking base-game APIs, entity models, action timing, combat behavior, Godot scene conventions, localization JSON shape, manifest behavior, or runtime resource paths.
- Use MinionLib when touching component cards, `ModComponentsCardTemplate`, component interfaces, dynamic vars, minion/component interactions, generated adapters, or behavior that comes from `FuYnAloft.Sts2.MinionLib`.
- Use STS2-RitsuLib when touching content scaffolding, auto-registration attributes, mod data registration, content packs, custom piles, audio helpers, keyword registration, or behavior that comes from `STS2.RitsuLib`.

If these external paths are missing or stale, ask the user before inventing replacement paths.
