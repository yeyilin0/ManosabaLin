using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Relics;

[RegisterRelic(typeof(SherrylinRelicPool))]
public sealed class MagnifyingGlass : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card.Owner != Owner) return;

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Count > 0) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        var discardPile = PileType.Discard.GetPile(Owner);

        var exhaustCards = exhaustPile.Cards.ToList();
        var discardCards = discardPile.Cards.ToList();

        if (exhaustCards.Count == 0 || discardCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        int swapCount = 0;

        foreach (var exhaustCard in exhaustCards)
        {
            var availableDiscard = discardPile.Cards.ToList();
            if (availableDiscard.Count == 0) break;

            var randomDiscard = rng.NextItem(availableDiscard);

            await CardPileCmd.RemoveFromCombat(exhaustCard);
            await CardPileCmd.Add(exhaustCard, PileType.Discard);

            await CardPileCmd.RemoveFromCombat(randomDiscard);
            await CardPileCmd.Add(randomDiscard, PileType.Exhaust);

            swapCount++;
        }

        if (swapCount > 0)
        {
            Flash();

            for (int i = 0; i < swapCount; i++)
            {
                await PowerCmd.Apply<XlmPower>(
                    choiceContext, Owner.Creature, 1,
                    Owner.Creature, null, false);
            }
        }
    }
}