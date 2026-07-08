using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheGreekInterpreter() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
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

        selectedCard.SetToFreeThisTurn();
        await CardPileCmd.Add(selectedCard, PileType.Hand);

        await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Unpowered, null);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
