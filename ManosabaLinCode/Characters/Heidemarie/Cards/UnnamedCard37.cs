using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard37() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string DiscardCountKey = "DiscardCount";

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar(DiscardCountKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var restCardsToDraw = DynamicVars.Cards.IntValue;
        if (restCardsToDraw > 0)
        {
            var restCards = PileType.Draw.GetPile(Owner).Cards
                .Where(card => card.HasComponent<RestComponent>())
                .Take(restCardsToDraw)
                .ToArray();

            await CardPileCmd.Add(restCards, PileType.Hand, clonedBy: this);
        }

        var discardCount = DynamicVars[DiscardCountKey].IntValue;
        if (discardCount <= 0)
            return;

        var selected = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, discardCount),
            null,
            this)).ToArray();

        if (selected.Length == 0)
            return;

        await CardCmd.Discard(choiceContext, selected);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars[DiscardCountKey].UpgradeValueBy(1m);
    }
}
