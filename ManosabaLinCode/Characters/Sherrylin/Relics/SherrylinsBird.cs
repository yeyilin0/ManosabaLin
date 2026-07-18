using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Relics;

[RegisterRelic(typeof(SherrylinRelicPool))]
public sealed class SherrylinsBird : MagnifyingGlass
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in HoverTipFactory.FromRelic<MagnifyingGlass>())
                yield return tip;

            foreach (var tip in RetainCounterComponent.Tip)
                yield return tip;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0) return;

        var selected = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
            null,
            this)).ToArray();
        if (selected.Length == 0) return;

        Flash();
        await CardCmd.Exhaust(choiceContext, selected[0]);
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (!participants.Contains(Owner.Creature)) return;

        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Select(card => new
            {
                Card = card,
                Component = (card as IComponentsCardModel)?.GetComponent<RetainCounterComponent>()
            })
            .Where(static entry => entry.Component is not null)
            .ToArray();
        if (candidates.Length == 0) return;

        var selected = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (selected?.Component is null) return;

        selected.Component.IncrementCounter();
        Flash();
        await Task.CompletedTask;
    }
}
