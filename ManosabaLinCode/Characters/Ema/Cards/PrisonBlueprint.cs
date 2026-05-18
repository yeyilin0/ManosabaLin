using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>监牢的设计图 - 2费能力, 赞同附魔, 每打出赞同牌获得1格挡, 同伴获得2格挡</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class PrisonBlueprint : ManosabaEmalinCardTemplate
{
    public PrisonBlueprint() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.AgreeKeywordId.GetModCardKeyword() };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PrisonBlueprintPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<PrisonBlueprintPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}