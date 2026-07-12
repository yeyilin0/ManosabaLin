namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinThreeColorBookmark() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new BlockVar(3m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;

        var pools = sketchbook.RecordedPoolCount;
        if (pools <= 0) return;

        await CardPileCmd.Draw(choiceContext, pools * DynamicVars.Cards.IntValue, Owner);
        await CreatureCmd.GainBlock(Owner.Creature, pools * DynamicVars.Block.IntValue, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(1m);
    }
}
