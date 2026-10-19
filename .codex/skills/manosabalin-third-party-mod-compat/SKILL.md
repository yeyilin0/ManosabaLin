---
name: manosabalin-third-party-mod-compat
description: Audit whether a third-party Slay the Spire 2 mod can coexist with ManosabaLin at runtime — Harmony patch-target overlap, soft-dependency contract drift, registration/init windows, and silent-failure acceptance criteria. Also use for black-box triage when the user only has a player-side godot.log plus the other mod's shipped package (no source), and wants the errors attributed and written up for the mod author. Use when the user hands over another mod's source (e.g. HextechRunes), a godot.log with another mod's errors, or asks whether a cross-mod feature will actually trigger.
---

# Third-Party Mod Coexistence Audit (ManosabaLin)

Use this when auditing coexistence with another STS2 mod: patch collisions, soft-dependency
contracts, init-order windows, and how failures surface.

**Read-only by default.** Produce a report; do not change code unless the user names the item.
If a prior audit report exists (usually `C:\Users\Lenovo\Desktop\*-verdict.md`), read it first and
only report what is *new* — do not re-state settled conclusions.

Ground rules for this repo: cross-mod code lives in `ManosabaLinCode/Compat/<Mod>/` and must be
**zero compile-time reference** to the other mod (reflection + string names only). Never Harmony-patch
the other mod's `internal` types. See `.workbuddy/memory/ENGINE-API.md` and `PITFALLS.md`.

## Step 1 — Harmony patch-target overlap

Run the scanner over our tree and theirs:

```bash
python .codex/skills/manosabalin-third-party-mod-compat/scripts/harmony_overlap_scan.py \
  ManosabaLinCode \
  "<other mod>/src"
```

For each `Type|method` in the intersection, classify it:

| Class | Meaning | Risk |
|---|---|---|
| **Both non-skipping** | both are prefix-void / postfix | none |
| **Stacking prefixes** | both `Prefix`, neither guarantees `return true` first | order-dependent |
| **Skip vs skip** | both may `return false` on the same input | one silently wins |

**Harmony priority rules that decide the outcome (verify, do not assume):**
- full enum: `Last=0, VeryLow=100, Low=200, LowerThanNormal=300, Normal=400, HigherThanNormal=500, High=600, VeryHigh=700, First=800`.
  An unannotated patch method is `Priority.Normal` = **400**; `[HarmonyPriority(Priority.Low)]` = **200**.
  ⚠️ Do not guess the numeric value — read `Priority` out of the decomp (`grep -n -B2 -A14 "enum Priority"`).
- a **`void` prefix can never skip** the original. Only a prefix whose declared return type is `bool`
  and which has a `return false;` on some path can. The scanner reads the *declaration form* precisely
  to avoid flagging `void` prefixes as skippable.
- **as soon as one prefix returns `false`, all remaining prefixes AND the original are skipped.**
- so: high-priority prefix that returns `false` ⇒ the low-priority one never runs ⇒ its
  bookkeeping/side effects are lost even though "nothing crashed".
- equality ⇒ Harmony uses patch order (install order), so prefer removing the tie rather than relying on it.

Report each real collision with: the input that triggers it, what the loser fails to do, and whether
it is *deterministic* (a real behavior gap) or *racy* (a timing bug). Deterministic + low probability
is normally acceptable — say so instead of inflating it.

## Step 2 — Soft-dependency contract drift

For every reflected call, confirm the target still exists with the **exact** parameter list:

- `GetMethod(name, Public|Static, null, types: [...], null)` returns `null` on any mismatch —
  a renamed/added parameter is a **silent** capability loss unless the code logs it.
- Check the other mod's assembly name (often pinned in its `.csproj` as `<AssemblyName>`), not its
  manifest id — variants may ship the same assembly name.
- Enumerate *all* overloads of the patched method (`grep -n "public static" …` in the decomp) and make
  sure our `[HarmonyPatch(typeof(X), "Y", [types])]` matches one exactly, including generic vs non-generic.

## Step 3 — Init / registration windows

Third-party registration APIs usually guard on a frozen state. Find the guard and locate it in the
call sequence:

- locate the throw (`InvalidOperationException` "too late" / "window closed") and read **what it
  protects** (model pool? serialization cache?).
- check **atomicity**: is the real registration the *last* statement? If validation runs first and
  registration last, failure is a clean "not registered" and not a half-registered wreck — verify by
  reading the entry point, don't infer.
- narrow the blast radius: a guard that only trips for types carrying a serialization attribute
  (e.g. `[SavedProperty]`) affects **only those types** — list which of ours qualify, don't say "all".
- our side should register **per item in try/catch** and log a summary count
  (e.g. `ok=9/9`). Silent partial registration is the most expensive failure mode: the game runs fine
  and the content just never appears.

**When the window is a runtime race, the acceptance criterion is the log line, not the code.**
State the exact string to grep for and what each failure variant means.

### 3b — Do NOT trust `AppDomain.AssemblyLoad` as "the target is ready"

A third-party mod may ship **two** assemblies: a small `<Mod>.Loader` (itself a `[ModInitializer]`)
that picks a per-game-version variant and then, **inside its own initializer**, does
`AssemblyLoadContext.LoadFromAssemblyPath(<Mod>.dll)` and only afterwards calls the real
`<Mod>.ModEntry.Initialize()`. Consequences:

- If our initializer runs first, our `AssemblyLoad` handler fires at the `LoadFromAssemblyPath`
  moment — i.e. **before** the target has installed its own bootstrap/config/registry state.
  Registration at that instant is a coin flip on mod load order.
- It is **not** an ALC issue: verified empirically that `AppDomain.AssemblyLoad` *does* fire for
  custom-ALC `LoadFromAssemblyPath` — so the event is reliable, the *timing* is what is not.
- Nor can we force it: `Assembly.Load("<Mod>")` resolves against the default probing path, but the real
  dll usually lives under `lib/<variant>/` and is version-picked by that loader. Forcing the load both
  fails to find it **and** risks picking an incompatible variant, defeating the loader's guard.

**Fix pattern — a deterministic "late sweep", still inside the window:**

```csharp
// RitsuLib gives replayable game-lifecycle events; these fire after ALL mod initializers,
// yet before any run exists (so the relic/model pools are not frozen yet).
RitsuLibFramework.SubscribeLifecycleOnce<GameReadyEvent>(_ => Sweep("GameReady"));
RitsuLibFramework.SubscribeLifecycleOnce<MainMenuReadyEvent>(_ => Sweep("MainMenuReady"));
```

`SubscribeLifecycleOnce<T>(handler, replayCurrentState: true)` invokes the handler once, and
**replays synchronously if the event already fired** — so subscribing late is safe too. Make the sweep
idempotent (Step 3c) and log the outcome. Note the replay only triggers after the event has actually
been raised, so subscribing during mod init does not itself re-introduce the early-call hazard.

### 3c — Make the fix idempotent per item, and make under-registration LOUD

- Track registered items in a set keyed by type name; `RegisterAll()` skips ones already done and
  re-attempts only the failures. This is what makes the Step 3b retry safe (no double registration).
- Expose `AllRegistered` so the sweeps can cheaply no-op, and so diagnostics can print it.
- Per-item `try/catch` must keep the mod loading (never let one item break init) — but the **summary
  when not full must be `Error`, not `Warn`**, listing the exception type + message per failed item.
  A `Warn` for "8 of 9 registered" is exactly the silent-failure mode this whole skill exists to kill.
- Keep auxiliary registrations (labels / config section titles) behind a flag that is only marked done
  when the main registrations are complete, so a later sweep redoes them for late-arriving items.

## Step 4 — Multiplayer / determinism review

Outside instructions say the same thing as our `mp-desync` rules; apply them:

- any "random" must come from `Owner.RunState.Rng.*`; flag `Random.Shared` and **indexing into a list
  whose order comes from registration order** (that order is mod-load-order dependent) — a
  `NextItem(unlockedCards)` can desync two clients with identical mod sets.
- turn hooks must confirm the holder via `participants` / `player != Owner`, because a teammate's
  extra turn re-enters the hook for that player.
- "first round of combat" ⇒ `PlayerCombatState.TurnNumber`, not `RoundNumber`.

## Step 5 — Report

Write to `C:\Users\Lenovo\Desktop\<mod>-compat-possible-issues.md` (or the path the user names).
Lead with a severity-sorted table (`file:line` evidence per row), then per-item detail with
**trigger / consequence / confirmation status**. Separate:

- *confirmed from source* vs *static inference, unverified* — the user runs no headless tests, so be
  explicit about which is which and never dress inference up as certainty;
- *real bug* vs *doc drift* vs *accepted trade-off / by design*.

End with a short "我建议的动手顺序" table and a reminder that nothing was changed without approval.

## Step 6 (black-box) — Triage another mod's runtime errors from a player-side log

Use when there is **no source**: only a full `godot.log` plus the other mod's shipped package
(`steamapps/workshop/content/2868840/<id>/`). Goal: attribute every warning/error to the right
mod, prove root cause with the shipped bytes, and write a paste-ready report for its author.
First worked example: `C:\Users\Lenovo\Desktop\limbus-hidden-boss-0.3.0-feedback.md` (Limbus
隐藏 Boss v0.3.0, 2026-10-15).

**Workflow**

1. **Pattern sweep + attribution.** `grep -n` the log for `^\[WARN\]|ERROR:` and group by the
   third-party namespace (e.g. `com.ritsukage.sts2-RitsuLib`, the mod's own Diagnostics tag).
   Before blaming the mod, check whether the same pattern also fires for *our* content or
   **vanilla** content — if yes it is a mechanism-wide noise (see pitfalls below), not a defect.
2. **PCK file-table parse (ground truth for "what's in the package").** The shipped `.pck` cannot
   lie. Parser pattern in `.local/limbus_pck_list.py` (also produces `.local/limbus_pck_files.txt`):
   - Godot 4.5 PCK **V3** header: `GDPC` magic; `u32 version(==3)`; `u32 ver_major/minor/patch`;
     `u32 pack_flags`; `u64 file_base`; `u64 dir_offset` → **seek to dir_offset** (near EOF!) and read
     `u32 file_count`, then entries `u32 path_len + bytes, u64 ofs, u64 size, 16B md5, u32 flags`.
   - ⚠️ `file_count` is NOT in the header for V3 — do not parse it inline with the header fields.
   - ⚠️ `path_len` bytes may carry `\x00`/padding — always `.strip("\x00 \t\r\n")` the key.
   - flags bit0 = DIR_ENCRYPTED (bail), bit1 = REL_FILEBASE (`file_base += pack_start`).
   - `.godot/uid_cache.bin` inside the pack: `u32 entry_count`, then repeated
     `u64 uid + u32 len + path` — 51,572 entries in the Limbus pack. Useful to prove which UIDs the
     package itself registers.
3. **Decompile the variant DLL** (`ilspycmd -o <dir> -p lib/<ver>/<Mod>.dll`) and align log stack
   frames (which carry **the author's PDB source paths** like `D:\STS2ModProject\Xichun\...cs:270`)
   with real line numbers. That is the strongest possible evidence for the author.
4. **Headless repro with the real Godot** — `.local/godot_probe/` pattern: a throwaway project
   (`project.godot` + `SceneTree` script) that runs
   `ProjectSettings.load_resource_pack("<mod>.pck", true)` and then calls the suspect API directly
   (`ResourceLoader.get_dependencies/exists`). Reproduce the exact error, count occurrences and
   match them against the game log (per-scene 2 deps × 2 APIs = 4 was the smoking gun for Limbus).
   Run: `<godot_console.exe> --headless --path <probe> --script res://probe2.gd`.
5. **Write the author-facing report** (desktop, `<mod>-<version>-feedback.md`): environment table →
   severity overview table (level × count × functional impact) → per item: 现象 / 日志原文(行号) /
   根因（代码位置 + 机制） / 修法建议 / 影响评估 → appendices: full line-number index,
   "non-defects you can ignore" (so the author does not waste time), and the verification method.

**Attribution pitfalls (all hit in real rounds)**

- **`Invalid UID.` (resource_uid.cpp:183)** = an *INVALID_ID* (0xFFFF…) reached `get_id_path`; a
  registered-miss instead prints `Unrecognized UID: "uid://…"`. Root cause seen 2026-10-15: Godot
  4.4+ `ResourceLoader.get_dependencies()` returns mixed strings `uid://<id>::::res://<path>`;
  third-party code that feeds those strings **back into GetDependencies/Exists** fails
  `text_to_id()` at the `:` → INVALID_ID. Fix = split on `::::`, take the `res://` part.
- **RitsuLib encounter background contract**: `AssetProfile.BackgroundScenePath != null` forces the
  path-based branch, which **throws** (not degrades) when `res://scenes/backgrounds/<id_lower>/layers`
  is missing — it then falls back to the programmatic slot. If the mod also sets
  `UseProgrammaticCombatBackground = true`, the non-null profile field is redundant and only produces
  a per-combat WARN; recommend clearing it.
- **Monster SFX** are requested by convention `event:/sfx/enemy/enemy_attacks/<id>_attack|_cast|_die`
  (`MonsterModel.DeathSfx` in sts2). Missing events → RitsuLib Audio WARN, once per (operation, path).
- **`Missing localization key` DebugCompat warnings are usually scanning noise**: if the same sweep
  also reports *vanilla* base keys (e.g. `LEES_WAFFLE`, `LIFE_FORGE`), it is a base-key probe, not a
  real miss — say so explicitly in the report. A genuinely actionable variant is the
  `<id>.title` style query from third-party stats mods (RitsuMetrics) against mod-prefixed keys.
- `Asset not cached` for a mod image is only a preload miss — check whether vanilla assets show the
  same line in the same window before reporting it as the mod's issue.

## Step 7 — Patching another mod's method whose signature uses ITS internal types

Situation: the only seam for a required behavior is a method that returns (or takes) a type **internal
to the other mod's assembly** — e.g. HextechRunes `HextechCatalog.GetCharacterRuneGroups()` returns
`IReadOnlyList<RuneSeriesGroup>` with `RuneSeriesGroup` internal. Not referencable at compile time, and
the other mod's guide says "no patches". Treat this as a **last resort** after its public API is
exhausted; get the user's explicit go-ahead, and only ever patch **display/derivation** methods
(never anything feeding sim state or a sync hash).

**`ref object __result` works — by documented rule, not luck.** The game's own `0Harmony.dll`
(`0Harmony.decompiled.cs:3922-3930`) accepts an injected `__result` whose declared type satisfies
`declaredType.IsAssignableFrom(returnType)`; `object` accepts everything (for value-type returns there
is separate boxing handling). **Ship-blocker rule: run the probe against the game's own 0Harmony first.**

```bash
cd .local/harmony_probe && dotnet run -c Release   # → [PASS] … RESULT: OK
```

The probe patches a `static IReadOnlyList<SomeInternalStruct>` target with `ref object __result`,
replaces the list via pure reflection, and asserts the caller sees the replacement — copy that project
as the template for any new such patch (it also covers the plain typed `ref IReadOnlyList<Type>` case).

Recipe (canonical implementation: `ManosabaLinCode/Compat/Hextech/HextechCharacterGroupsPatch.cs`):

1. `TryInstall(Assembly)` resolves everything by **string names** (`"<Mod>.<Type>"` + method name) —
   a `public static` member of an `internal` type is still reachable with `BindingFlags.Public|Static`;
   truly `internal` members need `|BindingFlags.NonPublic`.
2. **Shape-validate before patching.** Derive the element type from
   `method.ReturnType.GetGenericArguments()[0]`, assert the members you will construct exist
   (e.g. `LocalizationKey` / `Relics` props) and that the ctor `GetConstructor([...])` resolves.
   Any mismatch ⇒ don't patch at all; log once, latch `_unavailable` (no retry spam).
3. The postfix builds the replacement with **pure reflection**: copy existing items out of
   `(IEnumerable)current`, `ctor.Invoke([...])` for new items, then
   `Array.CreateInstance(elemType, n)` + `SetValue`. An array of `T` natively implements
   `IReadOnlyList<T>` — that is the whole trick; no IL emit needed.
4. Wrap the runtime body in try/catch: on failure leave the official result untouched and log **once**
   (latch). Never let an exception escape into the other mod's call path.
5. Downgrade gracefully for old versions: when the target methods are missing entirely, skip install
   with one explanatory line — the feature degrades, everything else keeps working.

## Pitfalls

- Do not conclude "will work" from the fact that a method resolves. Resolution + window + priority are
  three independent failures.
- Do not re-derive the other mod's semantics from our own `docs/`; read **its** source. Its
  `INTEGRATION.md` is usually more authoritative than our notes and may contain rules our docs lack.
- Delegating to the other mod's patch that we replace: when we `return false`, we must reproduce
  **every** branch of the original. Diff our replacement against the decompiled original line by line —
  dropped branches (e.g. an extra-payment hook) are the usual miss.
- When we replace a **base-game** model's hook to implement a compat effect (e.g. a Harmony prefix on a
  power's `AfterSideTurnEnd`), reproduce that hook's **own guard verbatim**. "Improving" it to the
  participants-based check silently changes vanilla behavior for every other holder of that power.
- Check *which hook stream actually reaches the model before claiming a hook is missing*:
  `RunState.IterateHookListeners(combatState)` **skips** run-level models (relics, potions) whenever
  `combatState != null` — but its last step chains `CombatState.IterateHookListeners()`, which **adds
  each player's relics**. So a relic's `AfterCombatEnd` / `BeforeCombatStart` *does* fire even though the
  run-level branch looked closed. Verify by reading both iterators before adding a workaround.
- A `static Dictionary<Player, …>` registry (hanging orbs, pending selections, …) keyed on `Player` is a
  lifetime leak unless it has an explicit combat-end (or run-end) clear. Turn-start cleanup is **not**
  enough: "still registered when the combat ends / the holder dies" is a separate path.
- A third-party "no-patch" pledge is a soft norm, not a wall: when the user explicitly orders a patch,
  pick the most harmless seam (display-only, zero state), keep it shape-validated with
  degrade-and-log-once, and **write the exception down in our docs** next to the feature so a later
  session does not "fix" it back to zero-patch.
