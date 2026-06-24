using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class CrimsonEdgeReturnPact()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const decimal BounceAmount = 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return SwordTokenGeneration.AddCrimsonSwordsToHand(
            choiceContext,
            Owner,
            DynamicVars.Cards.IntValue,
            this,
            card =>
            {
                if (card is ICrimsonSwordCard)
                    card.TryAddComponent(new BounceComponent(BounceAmount));
            });
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }
}
