using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinBrainwashBacklashPower : ManosabaPowerTemplate
{
    internal const int BrainwashSilenceCost = 26;
    internal const int BrainwashSilenceCostIncrease = 13;

    private const int SilenceTaxThreshold = 13;
    private const int WitchificationOnThirdBacklash = 25;

    [SavedProperty] public int PendingSilenceGained { get; set; }
    [SavedProperty] public int RequiredSilenceCost { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(new IntVar("SilenceTaxThreshold", SilenceTaxThreshold));
            description.Add(new PowerVar<WithPower>(WitchificationOnThirdBacklash));
            description.Add(new IntVar("NextBrainwashSilenceCost", CurrentBrainwashSilenceCost));
            description.Add(new IntVar("BacklashSilenceCostIncrease", BrainwashSilenceCostIncrease));
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("SilenceTaxThreshold", SilenceTaxThreshold),
        new PowerVar<WithPower>(WitchificationOnThirdBacklash),
        new IntVar("NextBrainwashSilenceCost", 0),
        new IntVar("BacklashSilenceCostIncrease", BrainwashSilenceCostIncrease)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromCard<BlankPage>()
    ];

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power == this)
        {
            if (amount > 0)
                await ResolveBacklashThresholds(choiceContext, (int)Math.Max(0, Amount - amount), (int)Amount);

            RefreshBrainwashSilenceCostVars();
            Owner.GetPower<AnanlinBrainwashPower>()?.RefreshRequiredSilenceCostVar();
            return;
        }

        if (power is SilentPower && power.Owner == Owner && amount > 0 && Amount >= 3)
            await TaxSilenceGained(choiceContext, (int)amount);
    }

    private async Task ResolveBacklashThresholds(
        PlayerChoiceContext choiceContext,
        int previousAmount,
        int currentAmount)
    {
        if (previousAmount < 1 && currentAmount >= 1)
            await LosePeace(choiceContext, 1);

        if (previousAmount < 2 && currentAmount >= 2)
            await AddBlankPageToDrawPile();

        if (previousAmount < 3 && currentAmount >= 3)
        {
            await LoseAllPeace(choiceContext);
            await PowerCmd.Apply<WithPower>(
                choiceContext,
                Owner,
                WitchificationOnThirdBacklash,
                Owner,
                null,
                false);

            EnterBrainwashCostMode();
        }
    }

    internal int CurrentBrainwashSilenceCost =>
        Amount >= 3 ? Math.Max(BrainwashSilenceCost, RequiredSilenceCost) : 0;

    internal int BrainwashSilenceCostForDescription =>
        Math.Max(BrainwashSilenceCost, CurrentBrainwashSilenceCost);

    internal void IncreaseBrainwashSilenceCostAfterPaidUse()
    {
        RequiredSilenceCost = Math.Max(BrainwashSilenceCost, RequiredSilenceCost) + BrainwashSilenceCostIncrease;
        RefreshBrainwashSilenceCostVars();
    }

    private void EnterBrainwashCostMode()
    {
        if (RequiredSilenceCost <= 0)
            RequiredSilenceCost = BrainwashSilenceCost;

        RefreshBrainwashSilenceCostVars();
    }

    private void RefreshBrainwashSilenceCostVars()
    {
        if (DynamicVars.TryGetValue("NextBrainwashSilenceCost", out var nextBrainwashSilenceCost))
            nextBrainwashSilenceCost.BaseValue = CurrentBrainwashSilenceCost;
    }

    private async Task LosePeace(PlayerChoiceContext choiceContext, int amount)
    {
        if (amount <= 0) return;
        if (Owner.GetPower<AnanlinPeaceOfMindPower>() is not { } peace) return;

        var loss = Math.Min(amount, (int)peace.Amount);
        if (loss > 0)
            await PowerCmd.ModifyAmount(choiceContext, peace, -loss, Owner, null);
    }

    private async Task LoseAllPeace(PlayerChoiceContext choiceContext)
    {
        if (Owner.GetPower<AnanlinPeaceOfMindPower>() is not { } peace) return;
        if (peace.Amount > 0)
            await PowerCmd.ModifyAmount(choiceContext, peace, -peace.Amount, Owner, null);
    }

    private async Task AddBlankPageToDrawPile()
    {
        if (Owner.Player is not { } player) return;
        if (Owner.CombatState is not { } combatState) return;

        var blankPage = combatState.CreateCard<BlankPage>(player);
        await CardPileCmd.AddGeneratedCardToCombat(blankPage, PileType.Draw, player, CardPilePosition.Random);
    }

    private async Task TaxSilenceGained(PlayerChoiceContext choiceContext, int gainedSilence)
    {
        if (gainedSilence <= 0) return;
        if (Owner.GetPower<SilentPower>() is not { } silence) return;

        PendingSilenceGained += gainedSilence;
        var taxTriggers = PendingSilenceGained / SilenceTaxThreshold;
        if (taxTriggers <= 0) return;

        PendingSilenceGained %= SilenceTaxThreshold;
        var loss = Math.Min(taxTriggers * (int)Amount, (int)silence.Amount);
        if (loss > 0)
            await PowerCmd.ModifyAmount(choiceContext, silence, -loss, Owner, null);
    }
}
