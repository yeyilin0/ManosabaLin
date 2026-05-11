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
    private int MarkDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private int PoisonAttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private int FrenzyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int PoisonAmount => 6;
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 30);
    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);
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
        // === 第一组意图（HP > 50%）===
        var attack = new MoveState("ATTACK_MOVE", AttackMove,
            new SingleAttackIntent(AttackDamage));

        var poison = new MoveState("POISON_MOVE", PoisonMove,
            new DebuffIntent());

        var debuffShield = new MoveState("DEBUFF_SHIELD_MOVE", DebuffShieldMove,
            new AbstractIntent[] { new DebuffIntent(), new DefendIntent() });

        var mark = new MoveState("MARK_MOVE", MarkMove,
            new AbstractIntent[] { new SingleAttackIntent(MarkDamage), new CardDebuffIntent() });

        // === 第二组意图（HP <= 50%）===
        var poisonAttack = new MoveState("POISON_ATTACK_MOVE", PoisonAttackMove,
            new AbstractIntent[] { new SingleAttackIntent(PoisonAttackDamage), new DebuffIntent() });

        var witchBurn = new MoveState("WITCH_BURN_MOVE", WitchBurnMove,
            new AbstractIntent[] { new BuffIntent(), new DefendIntent(), new CardDebuffIntent() });

        var frenzy = new MoveState("FRENZY_MOVE", FrenzyMove,
            new AbstractIntent[] { new DebuffIntent(), new MultiAttackIntent(FrenzyDamage, 2) });

        // === 第一组循环：attack → poison → debuffShield → mark → attack ===
        attack.FollowUpState = poison;
        poison.FollowUpState = debuffShield;
        debuffShield.FollowUpState = mark;
        mark.FollowUpState = attack;

        // === 第二组循环：poisonAttack → witchBurn → frenzy → poisonAttack ===
        poisonAttack.FollowUpState = witchBurn;
        witchBurn.FollowUpState = frenzy;
        frenzy.FollowUpState = poisonAttack;

        var states = new MonsterState[]
        {
            attack, poison, debuffShield, mark,
            poisonAttack, witchBurn, frenzy
        };

        return new MonsterMoveStateMachine(states, attack);
    }

    public override async Task AfterAddedToRoom()
    {
        await Task.CompletedTask;

        await PowerCmd.Apply<GuardOnePhasePower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

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

        var withPower = Creature.GetPower<WithPower>();
        var shieldAmount = withPower?.Amount ?? 0m;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Creature, shieldAmount, ValueProp.Move, null);
    }

    private async Task MarkMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(MarkDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
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

    private async Task PoisonAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PoisonAttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var target in targets)
        {
            await PowerCmd.Apply<PoisonPower>(
                new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
        }
    }

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

        await DamageCmd.Attack(FrenzyDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);
    }
}