using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnfoldAurora() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string LinkedCardsPerAuroraSwordKey = "LinkedCardsPerAuroraSword";
    public const string AuroraSwordsKey = "AuroraSwords";

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new LinkComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(LinkedCardsPerAuroraSwordKey, 1m),
        new DynamicVar(AuroraSwordsKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var linkedCardCount = CountLinkedCardsForEffect();
        var linkedCardsPerSword = DynamicVars[LinkedCardsPerAuroraSwordKey].IntValue;
        var swordsPerBatch = DynamicVars[AuroraSwordsKey].IntValue;
        if (linkedCardsPerSword <= 0 || swordsPerBatch <= 0 || linkedCardCount < linkedCardsPerSword)
            return;

        var swordCount = linkedCardCount / linkedCardsPerSword * swordsPerBatch;
        await SwordTokenGeneration.AddAuroraSwordsToHand(choiceContext, Owner, swordCount, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private int CountLinkedCardsForEffect()
    {
        var hand = PileType.Hand.GetPile(Owner);
        var count = hand.Cards.Count(card => card.HasComponent<LinkComponent>());
        return hand.Cards.Contains(this) || !((CardModel)this).HasComponent<LinkComponent>() ? count : count + 1;
    }
}
