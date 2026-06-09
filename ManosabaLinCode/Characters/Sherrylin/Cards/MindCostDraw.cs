using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 耗念引牌：回合开始选择一张卡消耗然后抽一，升级抽二。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class MindCostDraw() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MindCostDrawPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<MindCostDrawPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars.Cards.IntValue,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
