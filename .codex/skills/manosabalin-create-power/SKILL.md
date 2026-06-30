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

Update all supported locales:

- `ManosabaLin/localization/eng/powers.json`
- `ManosabaLin/localization/zhs/powers.json`
- `ManosabaLin/localization/jpn/powers.json`

Use keys like:

```json
"MANOSABA_LIN_POWER_EXAMPLE_POWER.title": "Example",
"MANOSABA_LIN_POWER_EXAMPLE_POWER.description": "At the end of turn, remove this.",
"MANOSABA_LIN_POWER_EXAMPLE_POWER.smartDescription": "Removed at end of turn."
```

Use `smartDescription` when the base game surface benefits from a compact runtime description. Add selection prompt/custom suffixes only when code reads them.

## Art

`ManosabaPowerTemplate` resolves:

- Icon: `ManosabaLin/images/powers/<classname>.png`
- Big: `ManosabaLin/images/powers/big/<classname>.png`
- Fallbacks: `power.png`

## Checks

- Verify multiplayer-relevant logic uses awaited hooks and command APIs.
- Keep localization aligned across `eng`, `zhs`, and `jpn`.
- Run `dotnet build ManosabaLin.sln`.
