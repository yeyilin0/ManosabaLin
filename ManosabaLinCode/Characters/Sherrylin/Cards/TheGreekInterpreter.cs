using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheGreekInterpreter() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var exhaustCards = exhaustPile.Cards.ToList();
        if (exhaustCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var selectedCard = rng.NextItem(exhaustCards);
        if (selectedCard == null) return;

        await CardPileCmd.RemoveFromCombat(selectedCard);
        await CardPileCmd.Add(selectedCard, PileType.Hand);
        selectedCard.SetToFreeThisTurn();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}