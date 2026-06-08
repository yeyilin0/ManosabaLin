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

    public bool HasTriggeredThisTurn { get; set; }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        HasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        if (combatState.RoundNumber != 1) return;

        // 进入战斗时设置2个充能球位置
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

        // 战斗外不触发（比如事件中获得诅咒牌）
        if (Owner.Creature?.CombatState == null) return;

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Any())
        {
            Swapping.Remove(Owner);
            return;
        }

        // 防重入：正在翻案时不再触发
        if (!Swapping.Add(Owner)) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (!exhaustPile.Cards.Any()) return;

        var discardPile = PileType.Discard.GetPile(Owner);
        if (!discardPile.Cards.Any()) return;

        Flash();
        HasTriggeredThisTurn = true;

        var exhaustCards = exhaustPile.Cards.ToList();
        var rng = Owner.RunState.Rng.CombatCardSelection;
        int swapCount = 0;

        foreach (var exhaustCard in exhaustCards)
        {
            if (!discardPile.Cards.Any()) break;

            var randomDiscard = rng.NextItem(discardPile.Cards.ToList());

            await CardPileCmd.Add(randomDiscard, PileType.Exhaust, skipVisuals: true);
            await CardPileCmd.Add(exhaustCard, PileType.Discard, CardPilePosition.Random, skipVisuals: true);

            swapCount++;
        }

        // 消耗堆剩余的牌（弃牌堆不够换的）也移入弃牌堆
        var remainingExhaust = exhaustPile.Cards.ToList();
        foreach (var leftover in remainingExhaust)
        {
            await CardPileCmd.Add(leftover, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
            swapCount++;
        }

        // 每交换1张获得1层橘雪莉的魔法
        for (int i = 0; i < swapCount; i++)
        {
            await PowerCmd.Apply<XlmPower>(
                new ThrowingPlayerChoiceContext(), Owner.Creature, 1,
                Owner.Creature, null, false);
        }
    }
}
