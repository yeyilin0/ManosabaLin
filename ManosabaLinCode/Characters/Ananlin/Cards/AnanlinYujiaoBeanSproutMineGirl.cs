using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinYujiaoBeanSproutMineGirl()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    private const int RequiredPeace = 13;
    private const int PeacePerBonusEnergy = 6;
    private const int BaseEnergyGain = 1;
    private const int BaseDraw = 1;
    private const string RecordedPeaceKey = "RecordedPeace";
    private const string RequiredPeaceKey = "RequiredPeace";

    private int _recordedPeace;
    private int _timesPlayedThisCombat;

    [SavedProperty]
    public int RecordedPeace
    {
        get => _recordedPeace;
        set
        {
            _recordedPeace = Math.Max(0, value);
            RefreshVars();
        }
    }

    [SavedProperty]
    public int TimesPlayedThisCombat
    {
        get => _timesPlayedThisCombat;
        set => _timesPlayedThisCombat = Math.Max(0, value);
    }

    private bool IsComplete => RecordedPeace >= RequiredPeace;
    private int BonusEnergy => RecordedPeace / PeacePerBonusEnergy;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new UniqueComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new EnergyVar(BaseEnergyGain + BonusEnergy),
        new CardsVar(BaseDraw),
        new IntVar(RecordedPeaceKey, RecordedPeace),
        new IntVar(RequiredPeaceKey, RequiredPeace)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromCard<MarginPage>(),
        HoverTipFactory.FromCard<AnanlinBombDisposalExpert>()
    ];

    protected override (PileType, CardPilePosition) GetResultPileTypeAndPositionForCardPlayC()
    {
        return IsComplete
            ? (PileType.Exhaust, CardPilePosition.Bottom)
            : base.GetResultPileTypeAndPositionForCardPlayC();
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, BaseDraw, Owner);
        await this.AddMarginPageToHand(false);

        if (cardPlay.Target is { } target)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        DoubleCostForCombat();
        if (IsComplete)
            await CreateBombDisposalExpert();
    }

    protected override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants,
        ComponentContext componentContext)
    {
        if (side != Owner.Creature.Side) return;

        var peaceToRecord = this.PeaceOfMindAmount();
        if (peaceToRecord <= 0) return;

        RecordedPeace += peaceToRecord;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private void DoubleCostForCombat()
    {
        TimesPlayedThisCombat++;
        var cappedMultiplierStep = Math.Min(TimesPlayedThisCombat, 7);
        var nextCost = Math.Min(99, Math.Max(1, EnergyCost.Canonical) * (1 << cappedMultiplierStep));
        EnergyCost.SetThisCombat(nextCost);
    }

    private void RefreshVars()
    {
        if (DynamicVars.TryGetValue("Energy", out var energyVar))
            energyVar.BaseValue = BaseEnergyGain + BonusEnergy;
        if (DynamicVars.TryGetValue(RecordedPeaceKey, out var recordedPeaceVar))
            recordedPeaceVar.BaseValue = RecordedPeace;
    }

    private async Task CreateBombDisposalExpert()
    {
        if (CombatState is not { } combatState) return;

        var card = combatState.CreateCard<AnanlinBombDisposalExpert>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }
}
