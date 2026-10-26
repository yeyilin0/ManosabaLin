using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SharedExhaustPlay() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    /// <summary>每名其他友方被消耗的牌数。</summary>
    private const int ExhaustPerAlly = 2;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var exhausted = new List<CardModel>();
        foreach (var ally in AllyPlayers(includeSelf: false))
        {
            for (var i = 0; i < ExhaustPerAlly; i++)
            {
                var card = PickCardToExhaust(ally.Creature, exhausted);
                if (card == null) break;

                exhausted.Add(card);
                await CardCmd.Exhaust(choiceContext, card);
            }
        }

        foreach (var player in AllyPlayers(includeSelf: true))
        {
            foreach (var exhaustedCard in exhausted)
            {
                var copy = CombatState.CreateCard(exhaustedCard.CanonicalInstance, player);
                copy.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);

                try
                {
                    await CardCmd.AutoPlay(choiceContext, copy, PickTarget(copy));
                }
                catch (Exception ex)
                {
                    // 复制出来的牌可能是本 mod 里其它机制造出来的怪东西（能力牌等）。
                    // 单张复制牌失败不得让整张「共享耗尽演奏」停在出牌区，记录后继续。
                    Log.Warn($"[SharedExhaustPlay] 复制牌 {copy.Id} 自动打出失败：{ex}");
                }

                // 能力牌（Power）自动打出后引擎已在 OnPlayWrapper 收尾里把它移出战斗牌堆，
                // 此时 Pile 为 null，再调 RemoveFromCombat 会抛
                // InvalidOperationException("Card must be in a combat pile for it to be removed")。
                // 该异常会把外层 OnPlayWrapper 的收尾（把本卡送去消耗堆）整段跳过 ⇒ 卡永久留在
                // 出牌区、屏幕上一直挂着这张牌。
                if (copy.Pile is { IsCombatPile: true })
                {
                    await CardPileCmd.RemoveFromCombat(copy);
                }
            }
        }
    }

    private IEnumerable<Player> AllyPlayers(bool includeSelf)
    {
        return CombatState.Players
            .Where(p => (includeSelf || p != Owner)
                && p.Creature.Side == Owner.Creature.Side
                && p.Creature.IsAlive);
    }

    private CardModel? PickCardToExhaust(Creature ally, IReadOnlyCollection<CardModel> alreadyPicked)
    {
        var cards = PileType.Draw.GetPile(ally.Player).Cards
            .Concat(PileType.Discard.GetPile(ally.Player).Cards)
            .Where(static c => !SamePlaceTruth.IsSelectionLocked(c))
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
            .Where(c => !alreadyPicked.Contains(c))
            .ToList();

        return cards.Count == 0 ? null : Owner.RunState.Rng.CombatCardSelection.NextItem(cards);
    }

    private Creature? PickTarget(CardModel card)
    {
        if (card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy or TargetType.AllEnemies)
        {
            var enemies = CombatState.GetOpponentsOf(card.Owner.Creature).Where(e => e.IsAlive).ToList();
            return enemies.Count == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        }

        if (card.TargetType == TargetType.AnyPlayer)
        {
            var allies = CombatState.Creatures.Where(c => c.Side == card.Owner.Creature.Side && c.IsAlive).ToList();
            return allies.Count == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(allies);
        }

        return null;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
