using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class PawnRealization : ManosabaCardTemplate
{
    private const string MeruruAndEmaEffectHoverLocEntry = "MANOSABA_LIN_CARD_MERURU_AND_EMA_EFFECT";

    public PawnRealization() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Unpowered)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<MllmPower>();
            yield return HoverTipFactory.FromCard<MeruruAndEma>();
            yield return CardEffectHoverTipFactory.FromCard(
                ModelDb.Card<MeruruAndEma>(),
                MeruruAndEmaEffectHoverLocEntry);
            yield return HoverTipFactory.FromPower<MeruruAndEmaAccomplicePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var estrangement = bond?.Estrangement ?? 0;
        if (estrangement > 0)
        {
            await CreatureCmd.Damage(choiceContext, creature, estrangement, ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
        }

        var shieldAmount = estrangement * 2;
        if (shieldAmount > 0)
        {
            await CreatureCmd.GainBlock(creature, shieldAmount, ValueProp.Move, cardPlay);
        }

        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            await PowerCmd.Apply<MllmPower>(
                choiceContext, creature, 1, creature, this, false);
        }

        await TryFuseMeruruAndEma(choiceContext);
    }

    private async Task TryFuseMeruruAndEma(PlayerChoiceContext choiceContext)
    {
        var owner = Owner;
        if (owner.Creature.GetPower<BondPower>() is not { } bond) return;
        if (bond.Affinity + bond.Estrangement != 13) return;

        var deckCards = owner.Deck.Cards.ToList();
        if (deckCards.Any(static card => card is MeruruAndEma)) return;

        var deckPawnRealization = deckCards.OfType<PawnRealization>().FirstOrDefault();
        var deckSubstituteCost = deckCards.OfType<SubstituteCost>().FirstOrDefault();
        if (deckPawnRealization == null || deckSubstituteCost == null) return;

        var combatSubstituteCost = FindCombatSubstituteCost(owner);
        if (combatSubstituteCost == null) return;
        if (owner.Creature.CombatState is not { } combatState) return;

        await CardPileCmd.RemoveFromCombat(combatSubstituteCost);
        if (Pile?.IsCombatPile == true)
            await CardPileCmd.RemoveFromCombat(this);

        var combatCard = combatState.CreateCard<MeruruAndEma>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(combatCard, PileType.Hand, owner);

        await CardPileCmd.RemoveFromDeck(deckPawnRealization, showPreview: false);
        await CardPileCmd.RemoveFromDeck(deckSubstituteCost, showPreview: false);

        var permanentCard = owner.RunState.CreateCard<MeruruAndEma>(owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(permanentCard, PileType.Deck));
    }

    private static SubstituteCost? FindCombatSubstituteCost(Player owner)
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play })
        {
            var card = pileType.GetPile(owner).Cards.OfType<SubstituteCost>().FirstOrDefault();
            if (card != null) return card;
        }

        return null;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
