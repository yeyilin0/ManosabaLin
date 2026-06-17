using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Relics;

[RegisterRelic(typeof(SherrylinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Sherrylin))]
public sealed class MagnifyingGlass : ManosabaRelicTemplate
{
    private static readonly HashSet<Player> Swapping = [];

    public override RelicRarity Rarity => RelicRarity.Rare;

    public bool HasTriggeredThisCombat { get; set; }
    public int CaseReversalDiscardToExhaustCount { get; set; }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        if (combatState.RoundNumber != 1) return;

        HasTriggeredThisCombat = false;

        var queue = Owner.PlayerCombatState?.OrbQueue;
        if (queue != null)
        {
            int addAmount = 2 - queue.Capacity;
            if (addAmount > 0)
            {
                queue.AddCapacity(addAmount);
                await Task.Yield();
                NCombatRoom.Instance?
                    .GetCreatureNode(Owner.Creature)?
                    .OrbManager?
                    .AddSlotAnim(addAmount);
            }
        }
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != Owner) return;
        if (Owner.Creature?.CombatState == null) return;

        if (card.Type == CardType.Status || card.Type == CardType.Curse)
        {
            var exhaustPile = PileType.Exhaust.GetPile(Owner);
            if (exhaustPile.Cards.Contains(card))
            {
                Flash();
                await CardPileCmd.RemoveFromCombat(card);
            }
        }

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Any())
        {
            Swapping.Remove(Owner);
            return;
        }

        if (!Swapping.Add(Owner)) return;

        var discardPile = PileType.Discard.GetPile(Owner);
        var exhaustPileSwap = PileType.Exhaust.GetPile(Owner);

        if (!exhaustPileSwap.Cards.Any())
        {
            Swapping.Remove(Owner);
            return;
        }

        HasTriggeredThisCombat = true;

        var exhaustCards = exhaustPileSwap.Cards.ToList();
        var discardCards = discardPile.Cards.ToList();

        CaseReversalDiscardToExhaustCount = discardCards.Count;

        foreach (var exhaustCard in exhaustCards)
        {
            await CardPileCmd.Add(exhaustCard, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
            await PowerCmd.Apply<XlmPower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature, 1,
                Owner.Creature, null, false);
        }

        foreach (var discardCard in discardCards)
        {
            await CardPileCmd.Add(discardCard, PileType.Exhaust, CardPilePosition.Random, skipVisuals: true);
        }
    }
}