# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Loop Master is a Unity color-block sorting puzzle game. Players pull blocks from trays onto a one-way conveyor belt; blocks are auto-absorbed into matching trays as they pass. The full gameplay spec — tray/stack rules, conveyor behavior, mystery blocks, boosters (Undo/Shuffle/Expand), win/lose conditions, and mandatory coding principles — lives in `GAME_DESIGN.md` at the repo root. **Read it before implementing any gameplay logic.**

The most important rule from that spec (section 6): when a rule is ambiguous or not covered, ask the user — do not guess the behavior.

The project is at an early/scaffolding stage: `Assets/Scripts/` (the intended home for gameplay code) is currently empty, and `Assets/Scenes/GameScene.unity` only has bare scaffolding (camera rig, background, `GameRoot`/`Map` placeholders) — no gameplay has been implemented yet.

## Environment

- Unity Editor version: **2022.3.62f2** (LTS) — see `ProjectSettings/ProjectVersion.txt`. Open/build/run only through the Unity Editor (or `Unity.exe -batchmode` for CI-style invocations); there are no npm/make/CLI build scripts in this repo.
- Key packages (`Packages/manifest.json`): TextMeshPro, Timeline, Splines, Visual Scripting. `com.unity.test-framework` is present transitively (via Code Coverage) but no test assembly/`Tests` folder exists yet — Window → General → Test Runner will show nothing until one is added.
- No CI config, linter, or build scripts exist in this repo; don't invent commands for these.

## Architecture notes

- **`Assets/Scripts/`** — intended location for all new gameplay code (currently empty). There is no `.asmdef` anywhere in the project, so everything under `Assets/` compiles into the single default `Assembly-CSharp` assembly — new script class names must not collide with anything already compiled into that assembly (see next point).
- **`Assets/ASSET RIPPER/`** — a decompiled reference project (original mobile game this one is modeled after), imported via AssetRipper. Its `Scripts/Assembly-CSharp/*.cs` files (e.g. `Box.cs`, `Cube.cs`, `GameController.cs`, `PathArrow.cs`, ...) are **empty dummy stubs** — AssetRipper had no source `.dll`s to decompile, so each file is just a bare `MonoBehaviour` class with no logic. They are *not* usable as a logic reference. They still matter for two reasons: (1) their class names occupy the shared `Assembly-CSharp` namespace, so new gameplay scripts must avoid name clashes with them, and (2) the imported prefabs/scenes/assets under that folder reference these script GUIDs, so don't delete or rename them casually.
- **`Assets/_Prefabs/Game/`** — gameplay prefabs (currently just `GameBgDark.prefab`, used in `GameScene.unity`).
- **`Assets/Scenes/GameScene.unity`** — the single scene in the project.
- Per `GAME_DESIGN.md` principle #5, keep level data (tray/color/order/capacity definitions) separate from gameplay logic so new levels can be authored without touching code.
