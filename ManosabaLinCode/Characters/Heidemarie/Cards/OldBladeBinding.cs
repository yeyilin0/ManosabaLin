using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class OldBladeBinding()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new LinkComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var candidates = PileType.Discard.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (candidates.Count == 0)
            return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var cardsToMove = Math.Min(DynamicVars.Cards.IntValue, candidates.Count);
        for (var i = 0; i < cardsToMove; i++)
        {
            var card = rng.NextItem(candidates);
            candidates.Remove(card);

            card.TryAddComponent(new LinkComponent());
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
