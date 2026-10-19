---
name: manosabalin-create-power
description: Create or modify powers, buffs, debuffs, and PowerModel behavior in the ManosabaLin Slay the Spire 2 mod. Use when adding a power class, choosing PowerType or PowerStackType, editing power hooks, power icons, dynamic vars, smart descriptions, or power localization.
---

# ManosabaLin Create Power

## Workflow

1. Read `AGENTS.local.md`; inspect base `PowerModel`, nearby powers, and RitsuLib scaffolding when unsure.
2. Put common powers under `ManosabaLinCode/Characters/Common/Powers/`; character-specific powers go under `Characters/<Character>/Powers/`.
3. Inherit `ManosabaPowerTemplate`.
4. Add `[RegisterPower]`.

```csharp
[RegisterPower]
public sealed class ExamplePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
```

## Common Power Pieces

- RitsuLib public entry is `MANOSABA_LIN_POWER_<TYPE_NAME>`.
- Define `PowerType` and `PowerStackType` explicitly.
- Use `CanonicalVars` when descriptions need dynamic values.
- Use base hooks such as `BeforeApplied`, `AfterApplied`, `AfterRemoved`, `AfterSideTurnStart`, `AfterSideTurnEnd`, `AfterCardPlayed`, `ModifyDamageAdditive`, `TryModifyPowerAmountReceived`.
- `ManosabaPowerTemplate.AfterPowerAmountChanged` already removes powers whose amount drops below zero unless `AllowNegative` is true.
- Use `PowerCmd.Apply<TPower>(choiceContext, target, amount, applier, cardSource)` and `PowerCmd.Remove(this)`.
- Check owner and side guards carefully: `Owner`, `Owner.Side`, `Owner.Creature`, and `target.Player` may matter.

## Localization

Update all 5 supported locales (zhs is the source language):

- `ManosabaLin/localization/eng/powers.json`
- `ManosabaLin/localization/zhs/powers.json`
- `ManosabaLin/localization/jpn/powers.json`
- `ManosabaLin/localization/kor/powers.json`
- `ManosabaLin/localization/rus/powers.json`

⚠️ Never rewrite these files with `json.dump` — BOM presence and indentation differ per file/locale. Edit line-by-line (or byte-level) and match each line's *actual* newline escaping (`\n` in one entry, `\\n` in the next — inconsistent even inside one file). Verify with `json.loads(bytes.decode('utf-8-sig'))` afterwards.

Use keys like:

```json
"MANOSABA_LIN_POWER_EXAMPLE_POWER.title": "Example",
"MANOSABA_LIN_POWER_EXAMPLE_POWER.description": "At the end of turn, remove this.",
"MANOSABA_LIN_POWER_EXAMPLE_POWER.smartDescription": "Removed at end of turn."
```

Use `smartDescription` when the base game surface benefits from a compact runtime description. Add selection prompt/custom suffixes only when code reads them.

### State-dependent descriptions (e.g. "different text before/after upgrade")

Only two engine paths exist, and they are not interchangeable:

- `PowerModel.GetDumbHoverTip` (the power icon tooltip) reads **`Description`** only.
- `PowerModel.SmartDescription` is used by `HoverTips` **only when `HasSmartDescription && IsMutable`**.

⇒ To make a power's text change with state (upgraded, locked color, stack count, …) **override BOTH** `Description` and `SmartDescriptionLocKey`, switching the key by state:

```csharp
public override LocString Description =>
    new LocString("powers", <COND> ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");

protected override string SmartDescriptionLocKey =>
    <COND> ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";
```

- Key lookup is a plain runtime dictionary lookup — **no whitelist**. Any suffix works **as long as the code explicitly builds that key and the JSON has it**.
- ⚠️ Never invent a suffix hoping the engine composes it: there is **no** built-in `Upgraded` key family (`descriptionUpgraded` is never queried — user-rejected on 10-04). `{IfUpgraded:...}` placeholders exist for **cards** only, not powers.
- Existing project precedents: `MeruruAndEmaAccomplicePower` (stack-count tiers `description2`/`description3`…), `BoundPrometheusPower` (`descriptionLocked`), the 13 powers using `descriptionEnhanced`.
- If only one state has custom text, keep the base key and add only the variant (smart channel falls back to `Description` automatically when its key is missing).

## Presentation Throttling

A power that resolves **N times in a loop** (N can reach dozens) must not play the full animation for every
iteration — it feels tedious. The engine already ships three switches for "resolve quickly in sequence";
**use them instead of patching anything**.

| To suppress | Switch |
|---|---|
| the target's hurt (`Hit`) animation | add `ValueProp.SkipHurtAnim` to the damage props (`0x10`) |
| the wait after gaining block | `CreatureCmd.GainBlock(creature, amount, props, cardPlay, fast: true)` → wait drops `0.25s` → `0.03s` |
| the flash + wait of a power amount change | `PowerCmd.Apply<T>(..., silent: true)` / `PowerCmd.ModifyAmount(..., silent: true)` |

```csharp
private const int MaxPlayedHits = 10;
for (var i = 0; i < hitCount; i++)
{
    var animated = i < MaxPlayedHits;
    var props = animated ? ValueProp.Unpowered : ValueProp.Unpowered | ValueProp.SkipHurtAnim;
    await CreatureCmd.Damage(choiceContext, target, damage, props, Owner, null, null);
    await CreatureCmd.GainBlock(ally, 1m, ValueProp.Move, null, fast: !animated);
    await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 1m, Owner, null, silent: !animated);
}
```

- All three are **presentation only** — numbers, ordering, and every `Hook.*` judgment are untouched
  (nothing in `Hook` reads `SkipHurtAnim`). Deterministic on both ends ⇒ **no multiplayer desync risk**.
- `SkipHurtAnim` only skips `TriggerAnim(receiver, "Hit", 0f)`; damage numbers and hit sparks still spawn.
- ⚠️ Never "speed up" a loop by bypassing `CreatureCmd.Damage` / writing `CurrentHp` directly — that skips
  `Hook.ModifyDamage` / `ModifyHpLost` / `BeforeDamageReceived`.
- ⚠️ A cap like `MaxPlayedHits` is **per call**. If several upstream callers each invoke the power once
  (e.g. one trigger per card), the total still accumulates.
- Precedent: `Characters/Yalisalin/Powers/IgnitePower.cs` (`TriggerIgnite`). Details in
  `.workbuddy/memory/ENGINE-API.md` → "表现节流".

## Art

`ManosabaPowerTemplate` resolves:

- Icon: `ManosabaLin/images/powers/<classname>.png`
- Big: `ManosabaLin/images/powers/big/<classname>.png`
- Fallbacks: `power.png`

## Checks

- Verify multiplayer-relevant logic uses awaited hooks and command APIs.
- Keep localization aligned across `eng`, `zhs`, `jpn`, `kor`, `rus`.
- Build/publish with `dotnet publish ManosabaLin.csproj -c Release` (run once the game is closed — a running `SlayTheSpire2.exe` locks the mod DLL *and* the PCK).
