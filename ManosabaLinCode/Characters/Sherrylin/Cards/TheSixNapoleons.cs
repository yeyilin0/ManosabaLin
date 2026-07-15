using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheSixNapoleons() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string DivisorKey = "Divisor";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<XlmPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(DivisorKey, 3)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var xlmStacks = Owner.Creature.GetPowerAmount<XlmPower>();
        if (xlmStacks <= 0) return;

        var exhaustCount = (int)xlmStacks;

        var drawCards = PileType.Draw.GetPile(Owner).Cards.ToList();
        var handCards = PileType.Hand.GetPile(Owner).Cards.Where(c => c != source).ToList();
        var discardCards = PileType.Discard.GetPile(Owner).Cards.ToList();

        var allCards = new List<CardModel>();
        allCards.AddRange(drawCards);
        allCards.AddRange(handCards);
        allCards.AddRange(discardCards);

        if (allCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var cardsToExhaust = allCards.OrderBy(_ => rng.NextFloat()).Take(exhaustCount).ToList();

        var targetRarities = new List<CardRarity>();
        foreach (var card in cardsToExhaust)
        {
            var upgradedRarity = GetUpgradedRarity(card.Rarity);
            targetRarities.Add(upgradedRarity);
        }

        foreach (var card in cardsToExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        var divisor = source.DynamicVars[DivisorKey].IntValue;
        var genCount = cardsToExhaust.Count / divisor;
        if (genCount <= 0) return;

        for (var i = 0; i < genCount; i++)
        {
            var targetRarity = targetRarities[i];
            if (targetRarity == CardRarity.Ancient) continue;

            var poolCards = Owner.Character.CardPool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == targetRarity
                            && c.CanBeGeneratedInCombat
                            && c.Rarity != CardRarity.Basic
                            && c.Rarity != CardRarity.Ancient
                            && c.Rarity != CardRarity.Event)
                .ToList();

            if (poolCards.Count == 0) continue;

            var template = Owner.RunState.Rng.CombatCardGeneration.NextItem(poolCards);
            if (template == null) continue;
            var newCard = CombatState.CreateCard(template, Owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
        }
    }

    private static CardRarity GetUpgradedRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Basic => CardRarity.Common,
            CardRarity.Common => CardRarity.Uncommon,
            CardRarity.Uncommon => CardRarity.Rare,
            CardRarity.Rare => CardRarity.Rare,
            CardRarity.Event => CardRarity.Uncommon,
            _ => rarity
        };
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[DivisorKey].UpgradeValueBy(-1);
    }
}
