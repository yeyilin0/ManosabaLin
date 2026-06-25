using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
[RegisterCharacterStarterCard(typeof(Heidemarie))]
public sealed class CondensedAurora()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public const string ReturnThresholdKey = "ReturnThreshold";
    public const string AuroraSwordsKey = "AuroraSwords";

    private int _successfulReturns;
    private bool _discardedFromHandByDiscardCommand;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(ReturnThresholdKey, 1),
        new DynamicVar(AuroraSwordsKey, 1m)
    ];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    protected override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this))
            return Task.CompletedTask;

        _discardedFromHandByDiscardCommand =
            oldPileType == PileType.Hand && card.Pile?.Type == PileType.Discard;

        return Task.CompletedTask;
    }

    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this) || !_discardedFromHandByDiscardCommand)
            return;

        _discardedFromHandByDiscardCommand = false;

        var result = await CardPileCmd.Add(this, PileType.Hand, clonedBy: this);
        if (!result.success || Pile?.Type != PileType.Hand)
            return;

        if (IsUpgraded)
        {
            await SwordTokenGeneration.AddSwordsToCombat(
                choiceContext,
                Owner,
                Owner,
                SwordTokenKind.Aurora,
                DynamicVars[AuroraSwordsKey].IntValue,
                PileType.Discard,
                CardPilePosition.Bottom,
                this);
        }

        _successfulReturns++;
        if (_successfulReturns < DynamicVars[ReturnThresholdKey].IntValue)
            return;

        var combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var liberated = combatState.CreateCard<LiberatedAurora>(Owner);
        for (var i = 0; i < CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(liberated);

        await CardCmd.Transform(this, liberated);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[ReturnThresholdKey].UpgradeValueBy(1m);
        DynamicVars[AuroraSwordsKey].UpgradeValueBy(1m);
    }
}
