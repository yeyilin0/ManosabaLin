using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwentySeven() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust); }
    }

    // 固定基础值 1
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 从消耗堆选择卡牌返回手牌
        var exhaustPile = PileType.Exhaust.GetPile(source.Owner);
        var cardsToRetrieve = source.DynamicVars.Cards.IntValue;

        if (exhaustPile.Cards.Count == 0)
            return;

        var prefs = new CardSelectorPrefs(
            source.SelectionScreenPrompt,
            cardsToRetrieve == 1 ? 1 : 0,
            cardsToRetrieve
        );

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            exhaustPile.Cards,
            source.Owner,
            prefs
        );

        foreach (var card in selectedCards) await CardPileCmd.Add(card, PileType.Hand);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}
