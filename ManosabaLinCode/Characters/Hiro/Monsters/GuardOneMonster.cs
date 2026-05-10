using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
public sealed class GuardOneMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 180);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 180);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int PoisonAmount => 6;
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 100, 50);
    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_one.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // === Normal moves (HP > 50%) ===
        var attack = new MoveState("ATTACK_MOVE", AttackMove,
            new MultiAttackIntent(AttackDamage, 2));

        var poison = new MoveState("POISON_MOVE", PoisonMove,
            new DebuffIntent());

        var debuffShield = new MoveState("DEBUFF_SHIELD_MOVE", DebuffShieldMove,
            new AbstractIntent[] { new DebuffIntent(), new DefendIntent() });

        var mark = new MoveState("MARK_MOVE", MarkMove,
            new AbstractIntent[] { new MultiAttackIntent(AttackDamage, 2), new CardDebuffIntent() });

        // === Enraged moves (HP <= 50%) ===
        var poisonAttack = new MoveState("POISON_ATTACK_MOVE", PoisonAttackMove,
            new AbstractIntent[] { new MultiAttackIntent(AttackDamage, 2), new DebuffIntent() });

        var witchBurn = new MoveState("WITCH_BURN_MOVE", WitchBurnMove,
            new AbstractIntent[] { new BuffIntent(), new DefendIntent(), new CardDebuffIntent() });

        var frenzy = new MoveState("FRENZY_MOVE", FrenzyMove,
            new AbstractIntent[] { new DebuffIntent(), new MultiAttackIntent(AttackDamage, 2) });

        // === Conditional branch at 50% HP ===
        var branch = new ConditionalBranchState("HP_BRANCH");
        branch.AddState(poisonAttack, () => Creature.CurrentHp <= Creature.MaxHp / 2);
        branch.AddState(attack, () => Creature.CurrentHp > Creature.MaxHp / 2);

        // === Normal flow ===
        attack.FollowUpState = poison;
        poison.FollowUpState = debuffShield;
        debuffShield.FollowUpState = mark;
        mark.FollowUpState = branch;

        // === Enraged flow (loops) ===
        poisonAttack.FollowUpState = witchBurn;
        witchBurn.FollowUpState = frenzy;
        frenzy.FollowUpState = poisonAttack;

        var states = new MonsterState[]
        {
            attack, poison, debuffShield, mark,
            branch,
            poisonAttack, witchBurn, frenzy
        };

        return new MonsterMoveStateMachine(states, attack);
    }

    // === Normal Move 1: Attack ===
    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);
    }

    // === Normal Move 2: Poison + Witchification ===
    private async Task PoisonMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<PoisonPower>(
                new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
        }

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);
    }

    // === Normal Move 3: Frail + Vulnerable + Shield from With ===
    private async Task DebuffShieldMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(
                new ThrowingPlayerChoiceContext(), target, VulnerableAmount, Creature, null);
        }

        // Gain shield based on current WithPower stacks
        var withPower = Creature.GetPower<WithPower>();
        var shieldAmount = withPower?.Amount ?? 0m;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Creature, shieldAmount, ValueProp.Move, null);
    }

    // === Normal Move 4: Attack + Add WitchMark to player hand ===
    private async Task MarkMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);

        foreach (var target in targets)
        {
            var player = target.Player;
            if (player != null)
            {
                var card = CombatState.CreateCard<WitchMark>(player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        }
    }

    // === Enraged Move 1: Attack + Poison ===
    private async Task PoisonAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<PoisonPower>(
                new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
        }
    }

    // === Enraged Move 2: Add WitchBurn + WithPower + Shield ===
    private async Task WitchBurnMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var target in targets)
        {
            var player = target.Player;
            if (player != null)
            {
                var card = CombatState.CreateCard<WitchBurn>(player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        }

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        var withPower = Creature.GetPower<WithPower>();
        var shieldAmount = withPower?.Amount ?? 0m;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Creature, shieldAmount, ValueProp.Move, null);
    }

    // === Enraged Move 3: Frail + Vulnerable + 2x Attack ===
    private async Task FrenzyMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
            await PowerCmd.Apply<VulnerablePower>(
                new ThrowingPlayerChoiceContext(), target, VulnerableAmount, Creature, null);
        }

        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);
    }
}
