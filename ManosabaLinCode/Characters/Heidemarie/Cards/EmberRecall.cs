using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class EmberRecall() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string AuroraSwordsKey = "AuroraSwords";

    private const int RecallCount = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(AuroraSwordsKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var discardCards = PileType.Discard.GetPile(Owner).Cards.ToArray();
        if (discardCards.Length == 0)
            return;

        var selectedCards = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            discardCards,
            Owner,
            new CardSelectorPrefs(new LocString("cards", Id.Entry + ".selectionScreenPrompt"), 0, RecallCount))).ToArray();

        var selectedCard = selectedCards.FirstOrDefault();
        if (selectedCard == null)
            return;

        var result = await CardPileCmd.Add(selectedCard, PileType.Hand, clonedBy: this);
        if (!result.success)
            return;

        await SwordTokenGeneration.AddAuroraSwordsToHand(
            choiceContext,
            Owner,
            DynamicVars[AuroraSwordsKey].IntValue,
            this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[AuroraSwordsKey].UpgradeValueBy(1m);
    }
}
