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
            var card = PickCardToExhaust(ally.Creature);
            if (card == null) continue;

            exhausted.Add(card);
            await CardCmd.Exhaust(choiceContext, card);
        }

        foreach (var player in AllyPlayers(includeSelf: true))
        {
            foreach (var exhaustedCard in exhausted)
            {
                var copy = CombatState.CreateCard(exhaustedCard.CanonicalInstance, player);
                copy.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);
                await CardCmd.AutoPlay(choiceContext, copy, PickTarget(copy));
                await CardPileCmd.RemoveFromCombat(copy);
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

    private CardModel? PickCardToExhaust(Creature ally)
    {
        var cards = PileType.Draw.GetPile(ally.Player).Cards
            .Concat(PileType.Discard.GetPile(ally.Player).Cards)
            .Where(static c => !SamePlaceTruth.IsSelectionLocked(c))
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
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
