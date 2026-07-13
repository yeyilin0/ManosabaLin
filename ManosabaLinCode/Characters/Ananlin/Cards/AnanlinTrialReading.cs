using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinTrialReading() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>("FallbackSilence", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        var card = sketchbook.RollCombatCardFromRecordedPools();
        if (card is null)
        {
            await sketchbook.AddSilence(choiceContext, DynamicVars["FallbackSilence"].IntValue, this);
            return;
        }

        card.ExhaustOnNextPlay = true;
        card.AddKeyword(CardKeyword.Ethereal);
        if (IsUpgraded)
            card.SetToFreeThisTurn();

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
