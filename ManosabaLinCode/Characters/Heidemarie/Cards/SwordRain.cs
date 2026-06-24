using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
[RegisterCharacterStarterCard(typeof(Heidemarie))]
public sealed class SwordRain()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public const string AuroraSwordsKey = "AuroraSwords";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(1m, ValueProp.Move),
        new DynamicVar(AuroraSwordsKey, 1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (IsUpgraded)
        {
            await SwordTokenGeneration.AddAuroraSwordsToHand(
                choiceContext,
                Owner,
                DynamicVars[AuroraSwordsKey].IntValue,
                this);
        }
    }

    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this) || Pile?.Type != PileType.Hand)
            return;

        await SwordTokenGeneration.AddAuroraSwordsToHand(
            choiceContext,
            Owner,
            DynamicVars[AuroraSwordsKey].IntValue,
            this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars[AuroraSwordsKey].UpgradeValueBy(1m);
    }
}
