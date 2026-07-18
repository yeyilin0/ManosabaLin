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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Relics;

[RegisterRelic(typeof(SherrylinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Sherrylin))]
[RegisterTouchOfOrobasRefinement(typeof(SherrylinsBird))]
public class MagnifyingGlass : ManosabaRelicTemplate
{
    private static readonly HashSet<Player> Swapping = [];

    public override RelicRarity Rarity => RelicRarity.Starter;

    public bool HasTriggeredThisCombat { get; set; }
    public int CaseReversalDiscardToExhaustCount { get; set; }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

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

        // 状态/诅咒牌进入消耗堆时直接移除
        // 状态/诅咒/远古牌进入消耗堆时直接移除
        if (card.Type == CardType.Status || card.Type == CardType.Curse || card.Rarity == CardRarity.Ancient)
        {
            var exhaustPile = PileType.Exhaust.GetPile(Owner);
            if (exhaustPile.Cards.Contains(card))
            {
                Flash();
                await CardPileCmd.RemoveFromCombat(card);
            }
        }

        var drawPile = PileType.Draw.GetPile(Owner);
        var discardPile = PileType.Discard.GetPile(Owner);

        bool drawEmpty = !drawPile.Cards.Any();
        bool discardEmpty = !discardPile.Cards.Any();

        // 两种情况触发翻案：
        // 1. 抽牌堆为空
        // 2. 抽牌堆和弃牌堆都为空
        bool shouldTrigger = drawEmpty || (drawEmpty && discardEmpty);

        if (!shouldTrigger)
        {
            Swapping.Remove(Owner);
            return;
        }

        if (!Swapping.Add(Owner)) return;

        var exhaustPileSwap = PileType.Exhaust.GetPile(Owner);
        if (!exhaustPileSwap.Cards.Any())
        {
            Swapping.Remove(Owner);
            return;
        }

        HasTriggeredThisCombat = true;

        var exhaustCards = exhaustPileSwap.Cards
            .Where(c => c is not ICaseFileCard)
            .ToList();
        var discardCards = discardPile.Cards
            .Where(c => c is not ICaseFileCard)
            .ToList();
        int swapCount = 0;

        // 一一交换，任意一方空就停止
        while (exhaustCards.Count > 0 && discardCards.Count > 0)
        {
            var exhaustCard = exhaustCards[0];
            var discardCard = discardCards[0];

            exhaustCards.RemoveAt(0);
            discardCards.RemoveAt(0);

            await CardPileCmd.Add(exhaustCard, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
            await PowerCmd.Apply<XlmPower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature, 2,
                Owner.Creature, null, false);

            await CardPileCmd.Add(discardCard, PileType.Exhaust, CardPilePosition.Random, skipVisuals: true);

            swapCount++;
        }

        // 弃牌堆先空 → 剩余消耗牌全部移入弃牌堆
        if (discardCards.Count == 0)
        {
            foreach (var c in exhaustCards)
            {
                await CardPileCmd.Add(c, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
                await PowerCmd.Apply<XlmPower>(
                    new ThrowingPlayerChoiceContext(), Owner.Creature, 2,
                    Owner.Creature, null, false);
            }
        }

        CaseReversalDiscardToExhaustCount = swapCount;
    }
}
