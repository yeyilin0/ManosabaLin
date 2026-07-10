using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Ananlin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinCardTen : ManosabaCardTemplate
{
    public AnanlinCardTen() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<HiroMagicRevivePower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<SuspectPower>(1m),
        new PowerVar<HiroMagicRevivePower>(1m)
    ];

    [SavedProperty] public bool HasBeenPlayed { get; set; }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["SuspectPower"].BaseValue, source.Owner.Creature, source, false);

        await PowerCmd.Apply<HiroMagicRevivePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["HiroMagicRevivePower"].BaseValue, source.Owner.Creature, source, false);

        HasBeenPlayed = true;
    }

    protected override async Task AfterCombatEnd(CombatRoom _, ComponentContext componentContext)
    {
        var source = this;

        if (!HasBeenPlayed)
            return;

        var deckCards = PileType.Deck.GetPile(source.Owner).Cards.ToList();
        foreach (var card in deckCards)
            if (card is AnanlinCardTen)
                await CardPileCmd.RemoveFromDeck(card);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}