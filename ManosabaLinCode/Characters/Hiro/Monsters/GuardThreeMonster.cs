using System.Collections.Generic;
using System.Threading.Tasks;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardThreeMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 240, 220);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 240, 220);

    private int StrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int CleaveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int ExecuteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 18);
    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_three.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // === Phase 1 (HP > 50%) ===
        var strike = new MoveState("STRIKE_MOVE", StrikeMove,
            new SingleAttackIntent(StrikeDamage));

        var guardUp = new MoveState("GUARD_UP_MOVE", GuardUpMove,
            new AbstractIntent[] { new DefendIntent(), new BuffIntent() });

        var cleave = new MoveState("CLEAVE_MOVE", CleaveMove,
            new MultiAttackIntent(CleaveDamage, 2));

        var intimidate = new MoveState("INTIMIDATE_MOVE", IntimidateMove,
            new AbstractIntent[] { new DebuffIntent(), new DebuffIntent() });

        // === Phase 2 (HP <= 50%) ===
        var execute = new MoveState("EXECUTE_MOVE", ExecuteMove,
            new SingleAttackIntent(ExecuteDamage));

        var warCry = new MoveState("WAR_CRY_MOVE", WarCryMove,
            new AbstractIntent[] { new BuffIntent(), new BuffIntent() });

        var onslaught = new MoveState("ONSLAUGHT_MOVE", OnslaughtMove,
            new AbstractIntent[] { new MultiAttackIntent(StrikeDamage, 3), new DebuffIntent() });

        // Phase 1 loop
        strike.FollowUpState = guardUp;
        guardUp.FollowUpState = cleave;
        cleave.FollowUpState = intimidate;
        intimidate.FollowUpState = strike;

        // Phase 2 loop
        execute.FollowUpState = warCry;
        warCry.FollowUpState = onslaught;
        onslaught.FollowUpState = execute;

        var states = new MonsterState[]
        {
            strike, guardUp, cleave, intimidate,
            execute, warCry, onslaught
        };

        return new MonsterMoveStateMachine(states, strike);
    }

    public override async Task AfterAddedToRoom()
    {
        await Task.CompletedTask;
        await PowerCmd.Apply<GuardThreePhasePower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private async Task StrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StrikeDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task GuardUpMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await CreatureCmd.GainBlock(Creature, 15m, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private async Task CleaveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CleaveDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitCount(2)
            .Execute(null);
    }

    private async Task IntimidateMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        foreach (var target in targets)
        {
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
            await PowerCmd.Apply<WeakPower>(
                new ThrowingPlayerChoiceContext(), target, WeakAmount, Creature, null);
        }
    }

    private async Task ExecuteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ExecuteDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    private async Task WarCryMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
        await CreatureCmd.GainBlock(Creature, 10m, ValueProp.Move, null);
    }

    private async Task OnslaughtMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StrikeDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .WithHitCount(3)
            .Execute(null);
        foreach (var target in targets)
        {
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
        }
    }
}
