using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class TwinEdgeSlumber() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string BounceVar = "Bounce";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new DynamicVar(BounceVar, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await SwordTokenGeneration.AddAuroraSwordsToHand(
            choiceContext,
            Owner,
            DynamicVars.Cards.IntValue,
            this,
            ConfigureGeneratedSword);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        this.TryAddComponent(new RestComponent());
    }

    private void ConfigureGeneratedSword(CardModel card)
    {
        card.TryAddComponent(new BounceComponent(DynamicVars[BounceVar].BaseValue));
    }
}
