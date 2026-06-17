---
name: manosabalin-local-context
description: Create, repair, or validate ManosabaLin's AGENTS.local.md machine-specific path file. Use when AGENTS.local.md is missing, required local paths are TODO or invalid, or the Slay the Spire 2, MinionLib, or STS2-RitsuLib source/decompiled directories must be located before working on this content mod.
---

# ManosabaLin Local Context

Use this skill to create or repair `AGENTS.local.md` for a ManosabaLin checkout.

## Workflow

1. Run `scripts/create_agents_local.py` from this skill.
2. Read the script output.
3. If `python` is unavailable, use the fallback search commands below instead of stopping.
4. If any required path is missing or invalid after script or fallback search, ask the user for that path or ask them to clone/pull the missing repository.
5. Re-run the script with explicit arguments or edit `AGENTS.local.md` directly with the discovered/user-provided paths.
6. Re-read `AGENTS.local.md` before continuing the original task.

## Script

From the repository root:

```powershell
python .codex\skills\manosabalin-local-context\scripts\create_agents_local.py
```

Useful options:

```powershell
python .codex\skills\manosabalin-local-context\scripts\create_agents_local.py --force
python .codex\skills\manosabalin-local-context\scripts\create_agents_local.py --slay-the-spire-2 D:\RiderProjects\SlayTheSpire2
python .codex\skills\manosabalin-local-context\scripts\create_agents_local.py --minionlib D:\RiderProjects\MinionLib
python .codex\skills\manosabalin-local-context\scripts\create_agents_local.py --ritsulib D:\RiderProjects\STS2-RitsuLib
```

The script searches common local project roots near the checkout. It does not perform a full disk crawl.

## Fallback Without Python

If `python` is not installed or not on `PATH`, search from likely project roots with `rg` and PowerShell. Prefer `rg --files` when available; otherwise use `Get-ChildItem`.

From the parent directory of the checkout, try:

```powershell
rg --files -g "sts2.sln" -g "sts2.csproj" -g "MinionLib.sln" -g "MinionLib.csproj" -g "STS2-RitsuLib.sln" -g "STS2-RitsuLib.csproj" ..
```

Also check common Windows roots:

```powershell
rg --files D:\RiderProjects C:\RiderProjects $HOME\RiderProjects $HOME\source\repos $HOME\Projects -g "sts2.sln" -g "sts2.csproj" -g "MinionLib.sln" -g "MinionLib.csproj" -g "STS2-RitsuLib.sln" -g "STS2-RitsuLib.csproj"
```

If `rg` is unavailable, use:

```powershell
Get-ChildItem -Path ..,D:\RiderProjects,C:\RiderProjects,$HOME\RiderProjects,$HOME\source\repos,$HOME\Projects -Recurse -File -Include sts2.sln,sts2.csproj,MinionLib.sln,MinionLib.csproj,STS2-RitsuLib.sln,STS2-RitsuLib.csproj -ErrorAction SilentlyContinue
```

Map results to `AGENTS.local.md` as follows:

- A directory containing `sts2.sln` or `sts2.csproj` plus `project.godot` is `SlayTheSpire2SourcePath`.
- A directory containing `MinionLib.sln` or `MinionLib.csproj` is `MinionLibPath`.
- A directory containing `STS2-RitsuLib.sln` or `STS2-RitsuLib.csproj` is `RitsuLibPath`.

If a required repository cannot be found, tell the user exactly which path is missing and ask them to provide it or clone/pull the corresponding repository locally.

## Path Requirements

All three paths are required for this content mod:

- `SlayTheSpire2SourcePath`: required for base game APIs, models, action timing, scenes, localization shape, and runtime behavior.
- `MinionLibPath`: required because ManosabaLin depends on MinionLib component-card behavior and related generated adapters.
- `RitsuLibPath`: required because ManosabaLin depends on RitsuLib scaffolding, auto-registration, custom piles, content packs, keywords, and audio helpers.

Optional convenience paths may be written when detected or supplied:

- `Sts2ReferenceDllPath`
- `Sts2InstallPath`
- `GodotPath`
- `FmodStudioPath`
