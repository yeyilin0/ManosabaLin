using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class ChainSigilIgnition() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var hand = PileType.Hand.GetPile(Owner);
        var linkedCards = hand.Cards
            .Where(card => card.HasComponent<LinkComponent>())
            .ToArray();

        foreach (var card in linkedCards)
        {
            if (card.Pile?.Type != PileType.Hand || !card.HasComponent<LinkComponent>())
                continue;

            await CardCmd.AutoPlay(choiceContext, card, null);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
