#!/usr/bin/env python3
"""Create or repair AGENTS.local.md for the ManosabaLin repository."""

from __future__ import annotations

import argparse
import os
from pathlib import Path
from typing import Iterable


ROOT_MARKERS = ("ManosabaLin.sln", "ManosabaLin.csproj")


def find_repo_root(start: Path) -> Path:
    current = start.resolve()
    for candidate in (current, *current.parents):
        if all((candidate / marker).exists() for marker in ROOT_MARKERS):
            return candidate
    return current


def candidate_roots(repo_root: Path) -> list[Path]:
    roots: list[Path] = []
    for path in [
        repo_root.parent,
        Path("D:/RiderProjects"),
        Path("C:/RiderProjects"),
        Path.home() / "RiderProjects",
        Path.home() / "source" / "repos",
        Path.home() / "Projects",
    ]:
        if path.exists() and path.is_dir() and path not in roots:
            roots.append(path)
    return roots


def iter_child_dirs(roots: Iterable[Path]) -> Iterable[Path]:
    for root in roots:
        try:
            yield from (child for child in root.iterdir() if child.is_dir())
        except OSError:
            continue


def has_any(path: Path, names: Iterable[str]) -> bool:
    return any((path / name).exists() for name in names)


def find_sts2_source(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "slay" not in name and "sts2" not in name and "spire" not in name:
            continue
        if has_any(child, ("sts2.sln", "sts2.csproj")) and (child / "project.godot").exists():
            return child
    return None


def find_minionlib(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "minion" not in name:
            continue
        if has_any(child, ("MinionLib.sln", "MinionLib.csproj")):
            return child
    return None


def find_ritsulib(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        name = child.name.lower()
        if "ritsu" not in name:
            continue
        if has_any(child, ("STS2-RitsuLib.sln", "STS2-RitsuLib.csproj")):
            return child
    return None


def find_reference_dlls(roots: Iterable[Path]) -> Path | None:
    for child in iter_child_dirs(roots):
        if (child / "sts2.dll").exists() and (child / "0Harmony.dll").exists():
            return child
        data_dir = child / "data_sts2_windows_x86_64"
        if (data_dir / "sts2.dll").exists() and (data_dir / "0Harmony.dll").exists():
            return data_dir
    return None


def fmt(path: Path | str | None) -> str:
    if path is None:
        return "TODO"
    return str(path)


def is_valid_path(value: str | None) -> bool:
    return bool(value and value != "TODO" and Path(value).exists())


def existing_values(path: Path) -> dict[str, str]:
    values: dict[str, str] = {}
    if not path.exists():
        return values
    for line in path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped.startswith("- `") or "`:" not in stripped:
            continue
        key = stripped.split("`:", 1)[0].removeprefix("- `")
        value = stripped.split("`:", 1)[1].strip().strip("`")
        if value:
            values[key] = value
    return values


def choose(key: str, detected: Path | str | None, existing: dict[str, str], forced: bool) -> str:
    current = existing.get(key)
    if current and not forced and is_valid_path(current):
        return current
    if detected is not None:
        return fmt(detected)
    return current or "TODO"


def write_agents_local(repo_root: Path, values: dict[str, str]) -> None:
    content = f"""# AGENTS.local.md

This file is machine-specific and should not be committed. `AGENTS.md` requires agents to read this file after the shared project instructions.

## Required Paths

- `SlayTheSpire2SourcePath`: `{values["SlayTheSpire2SourcePath"]}`
- `MinionLibPath`: `{values["MinionLibPath"]}`
- `RitsuLibPath`: `{values["RitsuLibPath"]}`

## Optional Paths

- `Sts2ReferenceDllPath`: `{values["Sts2ReferenceDllPath"]}`
- `Sts2InstallPath`: `{values["Sts2InstallPath"]}`
- `GodotPath`: `{values["GodotPath"]}`
- `FmodStudioPath`: `{values["FmodStudioPath"]}`

## Notes

- Slay the Spire 2, MinionLib, and RitsuLib paths are all required for this content mod.
- Use the base game source/decompiled project for game APIs, runtime behavior, scenes, localization shape, and model signatures.
- Use MinionLib for component-card behavior, components, generated adapters, and related helper APIs.
- Use RitsuLib for scaffolding, auto-registration, content packs, card piles, keywords, audio helpers, and mod data registration.
- If any required path is missing or stale, use `.codex/skills/manosabalin-local-context` to recreate this file.
"""
    (repo_root / "AGENTS.local.md").write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", type=Path)
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--slay-the-spire-2", "--sts2-decompiled", dest="sts2_source", type=Path)
    parser.add_argument("--minionlib", type=Path)
    parser.add_argument("--ritsulib", type=Path)
    parser.add_argument("--sts2-reference-dlls", type=Path)
    parser.add_argument("--sts2-install", type=str)
    parser.add_argument("--godot", type=str)
    parser.add_argument("--fmod-studio", type=str)
    args = parser.parse_args()

    repo_root = (args.repo_root or find_repo_root(Path.cwd())).resolve()
    roots = candidate_roots(repo_root)
    local_file = repo_root / "AGENTS.local.md"
    existing = existing_values(local_file)

    detected_sts2 = args.sts2_source or find_sts2_source(roots)
    detected_minionlib = args.minionlib or find_minionlib(roots)
    detected_ritsulib = args.ritsulib or find_ritsulib(roots)
    detected_dlls = args.sts2_reference_dlls or find_reference_dlls(roots)

    values = {
        "SlayTheSpire2SourcePath": choose("SlayTheSpire2SourcePath", detected_sts2, existing, args.force),
        "MinionLibPath": choose("MinionLibPath", detected_minionlib, existing, args.force),
        "RitsuLibPath": choose("RitsuLibPath", detected_ritsulib, existing, args.force),
        "Sts2ReferenceDllPath": choose("Sts2ReferenceDllPath", detected_dlls, existing, args.force),
        "Sts2InstallPath": args.sts2_install or existing.get("Sts2InstallPath", "TODO"),
        "GodotPath": args.godot or existing.get("GodotPath", "TODO"),
        "FmodStudioPath": args.fmod_studio or existing.get("FmodStudioPath", "TODO"),
    }

    write_agents_local(repo_root, values)
    print(f"Wrote {local_file}")

    required = {"SlayTheSpire2SourcePath", "MinionLibPath", "RitsuLibPath"}
    missing: list[str] = []
    invalid: list[str] = []
    for key, value in values.items():
        if value == "TODO":
            status = "MISSING"
            if key in required:
                missing.append(key)
        elif Path(value).exists():
            status = "OK"
        else:
            status = "INVALID"
            if key in required:
                invalid.append(key)
        print(f"{status}: {key} = {value}")

    for key in missing:
        print(f"REQUIRED_PATH_MISSING: ask the user for {key}.")
    for key in invalid:
        print(f"REQUIRED_PATH_INVALID: ask the user for a valid {key}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
