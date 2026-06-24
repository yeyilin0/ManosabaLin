using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class SlumberBrand() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<SlumberBrandPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this,
            true);

        power?.AddLayer(IsUpgraded);
    }
}
