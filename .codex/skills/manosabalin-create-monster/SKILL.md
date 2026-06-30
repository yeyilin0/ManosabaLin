---
name: manosabalin-create-monster
description: Create or modify monsters in the ManosabaLin Slay the Spire 2 mod. Use when adding a MonsterModel, monster moves, intents, HP/ascension tuning, monster scenes, encounters, events, combat rewards, or monster localization.
---

# ManosabaLin Create Monster

## Workflow

1. Read `AGENTS.local.md`; check base game `MonsterModel`, RitsuLib `ModMonsterTemplate`, and nearby monsters.
2. Put monster classes under `ManosabaLinCode/Characters/<Character>/Monsters/`.
3. Inherit `ModMonsterTemplate` unless a local `Manosaba...MonsterTemplate` exists.
4. Add `[RegisterMonster]`.
5. Add or reuse a Godot creature scene under `ManosabaLin/scenes/monsters/`.

```csharp
[RegisterMonster]
public sealed class ExampleMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 52, 48);
    public override int MaxInitialHp => MinInitialHp;

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/example.tscn");

    protected override NCreatureVisuals? TryCreateCreatureVisuals() =>
        RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack = new MoveState("ATTACK_MOVE", AttackMove, new SingleAttackIntent(AttackDamage));
        return new MonsterMoveStateMachine([attack], attack);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
```

## Monster Moves

- Use stable uppercase move ids ending in `_MOVE`.
- Each `MoveState` needs a handler and an intent: `SingleAttackIntent`, `MultiAttackIntent`, `DebuffIntent`, `BuffIntent`, `DefendIntent`, `CardDebuffIntent`, or arrays of intents.
- Wire `FollowUpState` for deterministic patterns; inspect existing state machines before adding randomness.
- Use `CreatureCmd.TriggerAnim`, `DamageCmd.Attack`, `PowerCmd.Apply`, `CardPileCmd.AddGeneratedCardToCombat`, and other commands.
- Use `ThrowingPlayerChoiceContext` only when no interactive choice can occur.

## Localization

Update all supported locales:

- `ManosabaLin/localization/eng/monsters.json`
- `ManosabaLin/localization/zhs/monsters.json`
- `ManosabaLin/localization/jpn/monsters.json`

Use keys like:

```json
"MANOSABA_LIN_MONSTER_EXAMPLE_MONSTER.name": "Example",
"MANOSABA_LIN_MONSTER_EXAMPLE_MONSTER.moves.ATTACK_MOVE.title": "Strike"
```

Move ids in localization must match `MoveState` ids exactly.

## Assets And Wiring

- Scene path convention: `res://ManosabaLin/scenes/monsters/<name>.tscn`.
- Keep `.tscn` paths, images, and code references aligned.
- Encounter or event wiring may live outside the monster class; search existing `[RegisterEncounter]`, event, and room code before adding new registration.

## Checks

- Verify ascension scaling for HP and damage.
- Verify all move ids have localization in every locale.
- Run `dotnet build ManosabaLin.sln`.
