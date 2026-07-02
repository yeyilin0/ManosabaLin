// GuardThreeCombatSingleton.cs
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Afflictions;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterSingleton]
public sealed class GuardThreeCombatSingleton : SingletonModel
{
    private const int MaxHpLossPerPlayer = 50;

    public GuardThreeCombatSingleton()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override bool ShouldDie(Creature creature)
    {
        if (creature.Monster is not GuardThreeMonster) return true;
        return creature.GetPower<UncontrolledJusticePower>() == null;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature.Monster is not GuardThreeMonster monster) return;

        var justice = creature.GetPower<UncontrolledJusticePower>();
        if (justice == null) return;

        // ① 立即打断当前意图
        if (monster.MoveStateMachine?.States.TryGetValue("PHASE2_ATTACK", out var move) == true &&
            move is MoveState moveState)
        {
            monster.SetMoveImmediate(moveState, forceTransition: true);
        }

        // ② 回满血 + 失去 50×玩家人数 的血量上限
        int playerCount = creature.CombatState.Players.Count();
        int hpLoss = MaxHpLossPerPlayer * playerCount;
        creature.SetMaxHpInternal(creature.MaxHp - hpLoss);
        await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);

        // ③ 清除阶段1遗留：移除所有玩家的所有Debuff，清除所有侵蚀牌
        foreach (var player in creature.CombatState.Players)
        {
            foreach (var power in player.Creature.Powers.ToList())
            {
                if (power.Type == PowerType.Debuff)
                    await PowerCmd.Remove(power);
            }

            foreach (var card in CombatCards(player).Where(c => c.Affliction is ErosionAffliction).ToList())
                CardCmd.ClearAffliction(card);
        }

        // ④ 先加 ThirteenWaterIntelPower（1层），确认成功
        await PowerCmd.Apply<ThirteenWaterIntelPower>(
            new ThrowingPlayerChoiceContext(), creature, 1, creature, null);

        // ⑤ 如果没有 FusionStandPower，加 FusionStandPower（1层）
        if (creature.GetPower<FusionStandPower>() == null)
        {
            await PowerCmd.Apply<FusionStandPower>(
                new ThrowingPlayerChoiceContext(), creature, 1, creature, null);
        }

        // ⑥ 确认 ThirteenWaterIntelPower 存在后，移除 UncontrolledJusticePower
        if (creature.GetPower<ThirteenWaterIntelPower>() != null)
            await PowerCmd.Remove(justice);

        // ⑦ 播放转阶段动画
        await monster.EnterPhaseTwo();
    }

    private static IEnumerable<CardModel> CombatCards(Player player)
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .SelectMany(pile => pile.GetPile(player).Cards);
    }
}
