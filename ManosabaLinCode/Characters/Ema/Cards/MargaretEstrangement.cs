using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class MargaretEstrangement : ManosabaCardTemplate
{
    public MargaretEstrangement() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyPlayer) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        await PowerCmd.Apply<MgmPower>(
            choiceContext, creature, 1, creature, this, false);

        var targetPlayer = (cardPlay.Target ?? creature).Player ?? owner;
        var deckCards = PileType.Deck.GetPile(targetPlayer).Cards.ToList();
        if (deckCards.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardSelection;
        var sourceCard = rng.NextItem(deckCards);
        var clone = CombatState.CreateCard(sourceCard.CanonicalInstance, owner);

        if (bond != null && bond.Estrangement > bond.Affinity)
            clone.SetToFreeThisCombat();

        await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Hand, owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
