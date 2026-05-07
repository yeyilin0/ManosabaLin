using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Friend() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<XlmPower>();
            yield return HoverTipFactory.FromPower<HnmPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromCard<Xlm>(IsUpgraded);
            yield return HoverTipFactory.FromCard<Hnm>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);

        var xlmCard = CombatState.CreateCard<Xlm>(source.Owner);
        var hnmCard = CombatState.CreateCard<Hnm>(source.Owner);

        if (IsUpgraded)
        {
            CardCmd.Upgrade(xlmCard);
            CardCmd.Upgrade(hnmCard);
        }

        var options = new List<CardModel> { xlmCard, hnmCard };
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, options, source.Owner,
            new CardSelectorPrefs(new LocString("Friend", "选择一项能力"), 1)
        );

        var chosen = selected.FirstOrDefault();
        if (chosen != null)
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, source.Owner, CardPilePosition.Bottom);
    }

    protected override void OnUpgrade()
    {
    }
}