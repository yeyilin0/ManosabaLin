---
name: manosabalin-create-relic
description: Create or modify relics in the ManosabaLin Slay the Spire 2 mod. Use when adding a RelicModel, editing relic behavior, pool registration, relic art, counters, pickup effects, right-click relics, reward logic, or relic localization.
---

# ManosabaLin Create Relic

## Workflow

1. Read `AGENTS.local.md`; check base game, RitsuLib, or MinionLib sources when signatures or behavior are unclear.
2. Put relics under `ManosabaLinCode/Characters/<Character>/Relics/`.
3. Inherit `ManosabaRelicTemplate` unless a nearby relic shows a stronger pattern.
4. Register into the correct relic pool:

```csharp
[RegisterRelic(typeof(HiroRelicPool))]
public sealed class ExampleRelic : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override async Task BeforeCombatStart()
    {
        await PowerCmd.Apply<TempStrength>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, 1, Owner.Creature, null);
    }
}
```

## Common Relic Pieces

- RitsuLib public entry is `MANOSABA_LIN_RELIC_<TYPE_NAME>`.
- Override `Rarity`; examples use `RelicRarity.Common`, `Uncommon`, `Rare`, `Boss`, `Shop`, `Starter`, or `Ancient`.
- Use `HasUponPickupEffect` and `AfterObtained()` for acquisition rewards or deck changes.
- Use `AfterRemoved()` for cleanup.
- Use AbstractModel hooks for run/combat behavior: `BeforeCombatStart`, `AfterCombatEnd`, `AfterCardPlayed`, `AfterRoomEntered`, `AfterRewardTaken`, etc. Use `$manosabalin-abstract-model-hooks` before choosing a hook.
- For counters, check nearby relics first; base `RelicModel` exposes `ShowCounter`, `DisplayAmount`, and mutable state patterns.
- For right-click behavior, search existing `IEasyRightClickableRelic` uses before implementing.

## Localization

Update all supported locales:

- `ManosabaLin/localization/eng/relics.json`
- `ManosabaLin/localization/zhs/relics.json`
- `ManosabaLin/localization/jpn/relics.json`

Use keys like:

```json
"MANOSABA_LIN_RELIC_EXAMPLE_RELIC.title": "Example Relic",
"MANOSABA_LIN_RELIC_EXAMPLE_RELIC.description": "At the start of combat, gain 1 Strength.",
"MANOSABA_LIN_RELIC_EXAMPLE_RELIC.flavor": "Short flavor text.",
"MANOSABA_LIN_RELIC_EXAMPLE_RELIC.selectionScreenPrompt": "Choose a card"
```

Only add prompt/custom suffix keys that code reads with `new LocString("relics", Id.Entry + ".suffix")`.

## Art

`ManosabaRelicTemplate` resolves art from the class name lowercased:

- Icon: `ManosabaLin/images/relics/<classname>.png`
- Outline: `ManosabaLin/images/relics/<classname>_outline.png`
- Big: `ManosabaLin/images/relics/big/<classname>.png`
- Fallbacks: `relic.png`, `relic_outline.png`

Keep resource paths and code names in sync.

## Checks

- Avoid dependency version changes.
- Keep code scoped to the relic and any required helper/localization/art.
- Run `dotnet build ManosabaLin.sln`.
