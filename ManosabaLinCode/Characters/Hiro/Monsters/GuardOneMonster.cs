// GuardOneMonster.cs
using System;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Events;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Hiro.Rewards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardOneMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 240, 220);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 240, 220);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int MarkDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 9);
    private int PoisonAttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 10);
    private int FrenzyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
    private int PoisonAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 6);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 30);
    private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);
    private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 2);
    private int PoisonAttackPoisonAmount => 3;
    private const int WeakAmountWhenPoisoned = 1;

    /// <summary>防止 <see cref="AfterDeath" /> 被重复调用时重复发放战胜奖励。</summary>
    private bool _bossRewardGranted;

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
        // 阶段1
        var attack = new MoveState("ATTACK_MOVE", AttackMove,
            new SingleAttackIntent(AttackDamage));

        var poison = new MoveState("POISON_MOVE", PoisonMove,
            new DebuffIntent());

        var debuffShield = new MoveState("DEBUFF_SHIELD_MOVE", DebuffShieldMove,
            new AbstractIntent[] { new DebuffIntent(), new DefendIntent() });

        var mark = new MoveState("MARK_MOVE", MarkMove,
            new AbstractIntent[] { new SingleAttackIntent(MarkDamage), new CardDebuffIntent() });

        attack.FollowUpState = poison;
        poison.FollowUpState = debuffShield;
        debuffShield.FollowUpState = mark;
        mark.FollowUpState = attack;

        // 阶段2
        var poisonAttack = new MoveState("POISON_ATTACK_MOVE", PoisonAttackMove,
            new AbstractIntent[] { new SingleAttackIntent(PoisonAttackDamage), new DebuffIntent() });

        var witchBurn = new MoveState("WITCH_BURN_MOVE", WitchBurnMove,
            new AbstractIntent[] { new BuffIntent(), new DefendIntent(), new CardDebuffIntent() });

        var frenzy = new MoveState("FRENZY_MOVE", FrenzyMove,
            new AbstractIntent[] { new DebuffIntent(), new MultiAttackIntent(FrenzyDamage, 2) });

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

        // 已中毒的目标改为 1 层虚弱，避免毒层滚雪球；意图图标两种情况都是 Debuff，不需要动态切换。
        foreach (var target in targets)
        {
            if (target.GetPower<PoisonPower>() is { Amount: > 0 })
                await PowerCmd.Apply<WeakPower>(
                    new ThrowingPlayerChoiceContext(), target, WeakAmountWhenPoisoned, Creature, null);
            else
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
        var shieldAmount = (withPower?.Amount ?? 0m) / 2m;
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
                new ThrowingPlayerChoiceContext(), target, PoisonAttackPoisonAmount, Creature, null);
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
        var shieldAmount = (withPower?.Amount ?? 0m) * 2m / 5m;
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

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == Creature)
        {
            RunManager.Instance.State?.Acts.ElementAtOrDefault(2)?._rooms.Ancient = ModelDb.AncientEvent<WitchoftheIsland>();
            RunManager.Instance.State?.Acts.ElementAtOrDefault(1)?.SetBossEncounter(ModelDb.Get<GuardTwoBossEncounter>());
            RunManager.Instance.State?.Acts.ElementAtOrDefault(2)?.SetBossEncounter(ModelDb.Get<GuardThreeEncounter>());

            // 「战胜残骸首领」奖励：真正死亡时（wasRemovalPrevented=true 是「死亡被阻止」的那次调用）只发放一次。
            if (!wasRemovalPrevented && !_bossRewardGranted)
            {
                _bossRewardGranted = true;
                GrantBossVictoryRewards();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    ///     给本次战斗的每个玩家追加「从牌组中选择两张牌升级」奖励。
    ///     通过 <see cref="CombatRoom.AddExtraReward" /> 挂到战斗房间上，会在战斗结束后的奖励屏里
    ///     以独立条目出现（<see cref="GuardOneBossUpgradeReward.RewardsSetIndex" />=0，排在金币/药水/卡牌之前）。
    /// </summary>
    private void GrantBossVictoryRewards()
    {
        try
        {
            if (RunManager.Instance.State?.CurrentRoom is not CombatRoom room) return;

            foreach (var player in room.CombatState.Players)
            {
                room.AddExtraReward(player, new GuardOneBossUpgradeReward(player));
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[GuardOneMonster] GrantBossVictoryRewards failed: {ex.Message}");
        }
    }
}
