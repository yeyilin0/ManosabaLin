using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Components;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class Swordlight() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const decimal BounceAmount = 1m;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new SwordlightTurnStartComponent()
    ];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        this.TryAddComponent(new BounceComponent(BounceAmount));
    }
}
