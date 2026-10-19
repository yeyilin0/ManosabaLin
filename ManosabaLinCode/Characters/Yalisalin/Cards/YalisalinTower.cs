using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     高塔（0 费 能力・远古・自身）：
///     获得 100 层【魔女化】和 1 层【魔女仪式】；
///     把敌人的火色格改造成 12 格（改造后每 4 格一种颜色）；
///     本场战斗内，每当你打出带【余火】的牌，额外为你连接一张「其他角色」的牌；
///     打出时按所有牌里带【余火】的牌的费用之和获得等量格挡。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class YalisalinTower()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_YALISALIN_TOWER_EFFECT";

    public override int MaxUpgradeLevel => 1;

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 100 层【魔女化】、1 层【魔女仪式】
        await PowerCmd.Apply<WithPower>(choiceContext, owner.Creature, 100, owner.Creature, this, false);
        await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);

        // 把敌人的火色格改造成 12 格（本场战斗），并挂上「余火牌额外连接其他角色的牌」的持续效果
        await PowerCmd.Apply<YalisalinTowerPower>(choiceContext, owner.Creature, 1, owner.Creature, this, false);

        // 获得所有牌里带【余火】的牌的费用之和的等量格挡
        var block = TotalFireComponentCost(owner);
        if (block > 0)
            await CreatureCmd.GainBlock(owner.Creature, block, ValueProp.Move, cardPlay);
    }

    /// <summary>「所有牌」（手 / 抽 / 弃）里带【余火】的牌的费用之和。</summary>
    private static int TotalFireComponentCost(Player owner)
    {
        var total = 0;

        foreach (var card in YalisalinFireComponentRules.AllCombatCards(owner))
        {
            if (!YalisalinFireComponentRules.HasFireComponent(card))
                continue;

            if (card.EnergyCost.CostsX)
                continue;

            total += Math.Max(0, card.EnergyCost.GetWithModifiers(CostModifiers.All));
        }

        return total;
    }
}
