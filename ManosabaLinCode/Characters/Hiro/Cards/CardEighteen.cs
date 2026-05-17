using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardEighteen() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 需要消耗的伪证层数（固定 2 层）
    private const int RequiredPerjuryAmount = 2;
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<PerjuryPower>(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PerjuryPower>(); }
    }

    // 控制卡牌是否可打出
    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            var perjuryPower = Owner.Creature.GetPower<PerjuryPower>();
            var perjuryAmount = perjuryPower?.Amount ?? 0;

            return perjuryAmount >= RequiredPerjuryAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 第一步：消耗 2 层伪证
        // 第一步：消耗 2 层伪证
        var perjuryPower = source.Owner.Creature.GetPower<PerjuryPower>();
        if (perjuryPower != null && perjuryPower.Amount >= RequiredPerjuryAmount)
            await PowerCmd.ModifyAmount(
                choiceContext, // ★ 第一个参数
                perjuryPower,
                -RequiredPerjuryAmount,
                source.Owner.Creature,
                source
            );

        // 第二步：获得格挡
        await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);

        await Cmd.Wait(0.25f);
    }

    // 返回手牌而不是弃牌堆
    protected override PileType GetResultPileTypeForCardPlayC()
    {
        var resultPileType = base.GetResultPileTypeForCardPlayC();
        return resultPileType != PileType.Discard ? resultPileType : PileType.Hand;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3m); // 格挡 8 → 11
    }
}
