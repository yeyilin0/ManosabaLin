using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinYujiaoBeanSproutMineGirl()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    private const int RequiredPeace = 13;
    private const int PeacePerBonusEnergy = 6;
    private const int BaseEnergyGain = 1;
    private const int BaseDraw = 1;
    private const int PeaceForTurnEndReward = AnanlinPeaceOfMindPower.MaxStacks;
    private const string RecordedPeaceKey = "RecordedPeace";
    private const string RequiredPeaceKey = "RequiredPeace";

    private static readonly Type[] PeaceConsumerCardTypes =
    [
        typeof(AnanlinAfterDeepBreath),
        typeof(AnanlinGentleNod),
        typeof(AnanlinLover),
        typeof(AnanlinPushTheDoorOpen),
        typeof(AnanlinWeavingLiesSleepingPrincess)
    ];

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

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        return IsComplete
            ? new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlayC();
    }

    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        if (player != Owner) return;

        if (Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(this, PileType.Hand);

        await this.GainPeaceOfMind(choiceContext);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, BaseDraw, Owner);
        await this.AddMarginPageToHand(false);

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

    protected override bool HasTurnEndInHandEffectC => true;

    protected override async Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext,
        ComponentContext componentContext)
    {
        if (this.PeaceOfMindAmount() < PeaceForTurnEndReward) return;

        await AddRetainedPeaceConsumerCardToHand();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
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

    private async Task AddRetainedPeaceConsumerCardToHand()
    {
        if (CombatState is not { } combatState) return;

        var canonical = RollPeaceConsumerCard();
        if (canonical is null) return;

        var card = combatState.CreateCard(canonical, Owner);
        card.AddKeyword(CardKeyword.Retain);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    private CardModel? RollPeaceConsumerCard()
    {
        var candidates = PeaceConsumerCardTypes
            .Select(static type => ModelDb.GetByIdOrNull<CardModel>(ModelDb.GetId(type)))
            .OfType<CardModel>()
            .ToArray();
        if (candidates.Length == 0) return null;

        return Owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
    }
}
