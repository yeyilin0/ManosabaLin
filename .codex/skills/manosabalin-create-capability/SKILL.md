---
name: manosabalin-create-capability
description: Create or modify RitsuLib model capabilities in the ManosabaLin mod. Use when adding CardCapability, RelicCapability, default capabilities, capability persistence, card description contributors, hover tips, play-result overrides, glow/title/property contributors, or capability localization.
---

# ManosabaLin Create Capability

## Workflow

1. Read `AGENTS.local.md`; use the local RitsuLib source for capability APIs before guessing.
2. Put capability classes near the owning content, e.g. `Characters/Sherrylin/Capabilities/`.
3. For card-facing capabilities, inherit `ManosabaCardCapability`.
4. Register model-backed capabilities with `[RegisterModelCapability]`.

```csharp
[RegisterModelCapability]
public sealed class ExampleCapability : ManosabaCardCapability, ICardPlayResultContributor
{
    public PileType? GetResultPileTypeForCardPlay(CardModel card) => PileType.None;
}
```

Attach to a card with:

```csharp
card.GetOrCreateCapability<ExampleCapability>();
```

## Important Id Rule

Do not confuse the capability registry id with `Id.Entry`.

- `ModelCapabilityRegistry` capability id uses RitsuLib's `MODELCAPABILITY` segment.
- `AbstractModel.Id.Entry` uses RitsuLib public entry patch and the base model category: `MODEL_CAPABILITY`.
- `ManosabaCardCapability` localization reads `Id.Entry`.

For `RemoveOnPlayCapability`, localization keys use:

```text
MANOSABA_LIN_MODEL_CAPABILITY_REMOVE_ON_PLAY_CAPABILITY
```

not:

```text
MANOSABA_LIN_MODELCAPABILITY_REMOVE_ON_PLAY_CAPABILITY
```

## Card Text And Hover Tips

`ManosabaCardCapability` handles optional localized card text:

- `{Id.Entry}.beforeBase` inserts before the card base description.
- `{Id.Entry}.afterBase` inserts after the card base description.
- `{Id.Entry}.hovertip.title` and `{Id.Entry}.hovertip.description` add a hover tip only when both exist.
- `DynamicVars` and `AddExtraLocArguments(LocString loc)` are applied by the template.

Localization goes in `cards.json` because these are card text surfaces:

```json
"MANOSABA_LIN_MODEL_CAPABILITY_EXAMPLE_CAPABILITY.afterBase": "[gold]Remove[/gold]",
"MANOSABA_LIN_MODEL_CAPABILITY_EXAMPLE_CAPABILITY.hovertip.title": "Remove on Play",
"MANOSABA_LIN_MODEL_CAPABILITY_EXAMPLE_CAPABILITY.hovertip.description": "When played, remove this card from combat."
```

Update `eng`, `zhs`, and `jpn`.

## Common Interfaces

- `ICardDescriptionContributor`: add card description fragments.
- `ICardHoverTipContributor`: add hover tips.
- `ICardTitleContributor`: add or replace title fragments.
- `ICardGlowContributor`: gold/red hand glow.
- `ICardPropertyContributor`: override card type, rarity, target type, tags, costs where supported.
- `ICardPlayStateContributor`: can-play style checks.
- `ICardPlayResultContributor`: result pile and position.
- `ICardTransformCarryOverCapability`: carry capability across transforms.
- `IModelCapabilityHookListener`/`OwnerHookCapability<T>`: receive the owner's vanilla hooks.

## Checks

- Prefer `ManosabaCardCapability` for card-facing text instead of hand-written localization code.
- Ensure model-backed capabilities have parameterless construction and are registered before `GetOrCreateCapability<T>()`.
- Run `dotnet build ManosabaLin.sln`.
