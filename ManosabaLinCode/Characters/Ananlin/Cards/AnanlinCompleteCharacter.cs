namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinCompleteCharacter() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        if (!sketchbook.HasFullRecordedPools)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
            await CardPileCmd.RemoveFromCombat(this);
            return;
        }

        var card = sketchbook.RollCombatCardFromRecordedPools();
        if (card is null) return;

        if (IsUpgraded)
            CardCmd.Upgrade(card);

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
