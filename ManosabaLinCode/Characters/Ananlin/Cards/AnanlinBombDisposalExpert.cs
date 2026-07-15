using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class AnanlinBombDisposalExpert()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Ancient, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    private const int PeaceThreshold = 3;
    private const string PeaceThresholdKey = "PeaceThreshold";

    private int _turnsEndedWithThreePeace;
    private int _timesPlayedThisCombat;

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int TurnsEndedWithThreePeace
    {
        get => _turnsEndedWithThreePeace;
        set => _turnsEndedWithThreePeace = Math.Max(0, value);
    }

    [SavedProperty]
    public int TimesPlayedThisCombat
    {
        get => _timesPlayedThisCombat;
        set => _timesPlayedThisCombat = Math.Max(0, value);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(PeaceThresholdKey, PeaceThreshold),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("PeaceVigor").WithMultiplier((card, _) => ((AnanlinBombDisposalExpert)card).TurnsEndedWithThreePeace),
        new CalculatedVar("RewriteVigor").WithMultiplier((card, _) => ((AnanlinBombDisposalExpert)card).RewriteVigor())
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromPower<AnanlinVigorStaysPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<AnanlinVigorStaysPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);

        var peaceVigor = TurnsEndedWithThreePeace;
        if (peaceVigor > 0)
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                peaceVigor,
                Owner.Creature,
                this);
        }

        var rewriteVigor = RewriteVigor();
        if (rewriteVigor > 0)
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                rewriteVigor,
                Owner.Creature,
                this);
        }

        await AddPlayableMultiHitAttackToHand();
        DoubleCostForCombat();
    }

    protected override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        ComponentContext componentContext)
    {
        if (side == Owner.Creature.Side && this.PeaceOfMindAmount() >= PeaceThreshold)
            TurnsEndedWithThreePeace++;

        return Task.CompletedTask;
    }

    private async Task AddPlayableMultiHitAttackToHand()
    {
        if (CombatState is not { } combatState) return;

        var card = AnanlinCardHelpers.RollPlayableStableMultiHitAttack(
            Owner,
            combatState,
            setBaseCostToZero: true);
        if (card is null) return;

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    private int RewriteVigor()
    {
        return AnanlinSilenceIntentManager.GetRewritesThisCombat(Owner);
    }

    private void DoubleCostForCombat()
    {
        TimesPlayedThisCombat++;
        var cappedMultiplierStep = Math.Min(TimesPlayedThisCombat, 7);
        var nextCost = Math.Min(99, Math.Max(1, EnergyCost.Canonical) * (1 << cappedMultiplierStep));
        EnergyCost.SetThisCombat(nextCost);
    }
}
