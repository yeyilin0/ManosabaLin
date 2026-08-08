using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Utilities.BetterExtraArgs;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class MeruruAndEma() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_MERURU_AND_EMA_EFFECT";

    internal const int InitialAccompliceStacks = 1;
    internal const int AutoPlayThreshold = 7;
    internal const int MaxAccompliceStacks = MeruruAndEmaAccomplicePower.PreBreakthroughMaxStacks;

    [SavedProperty]
    public int AccompliceStacksToGain
    {
        get;
        set
        {
            AssertMutable();
            field = Math.Clamp(value, InitialAccompliceStacks, MaxAccompliceStacks);
            SyncDynamicVars();
        }
    } = InitialAccompliceStacks;

    [SavedProperty] public bool HasAutoPlayedThisCombat { get; set; }

    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new UniqueComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MeruruAndEmaAccomplicePower>("AccompliceStacks", InitialAccompliceStacks),
        new IntVar("AutoPlayThreshold", AutoPlayThreshold),
        new IntVar("MaxStacks", MaxAccompliceStacks)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry);
            yield return HoverTipFactory.FromPower<MeruruAndEmaAccomplicePower>();
        }
    }

    protected override Task BeforeCombatStart(ComponentContext componentContext)
    {
        HasAutoPlayedThisCombat = false;
        return Task.CompletedTask;
    }

    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        if (player != Owner) return;
        if (HasAutoPlayedThisCombat) return;
        if (AccompliceStacksToGain < AutoPlayThreshold) return;
        if (Pile?.IsCombatPile != true) return;

        if (Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(this, PileType.Hand);

        HasAutoPlayedThisCombat = true;
        this.SetFreeIgnoringCardPlayConditions();
        await CardCmd.AutoPlay(choiceContext, this, null, skipCardPileVisuals: true);

        if (Pile?.IsCombatPile == true)
            await CardPileCmd.RemoveFromCombat(this);

        await RemoveMeruruAndEmaCardsFromHand();
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        return new CardLocation(Owner, PileType.None, CardPilePosition.Bottom);
    }

    protected override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (!HasAutoPlayedThisCombat) return;
        if (card.Owner != Owner) return;
        if (card is not MeruruAndEma) return;
        if (card.Pile?.Type != PileType.Hand) return;
        if (card.HasBeenRemovedFromState) return;

        await CardPileCmd.RemoveFromCombat(card);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        SyncDynamicVars();

        await PowerCmd.Apply<MeruruAndEmaAccomplicePower>(
            choiceContext,
            Owner.Creature,
            AccompliceStacksToGain,
            Owner.Creature,
            this,
            false);

        if (Owner.Creature.GetPower<MeruruAndEmaAccomplicePower>() is { } accomplice)
            await accomplice.ResolveCardPlayedStage(choiceContext, this);

        await RemoveMeruruAndEmaCardsFromHand();
    }

    public void IncreaseAccompliceStacksToGain(int amount)
    {
        if (amount <= 0) return;
        AccompliceStacksToGain = Math.Min(MaxAccompliceStacks, AccompliceStacksToGain + amount);
    }

    private async Task RemoveMeruruAndEmaCardsFromHand()
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards
            .OfType<MeruruAndEma>()
            .Where(static card => !card.HasBeenRemovedFromState)
            .ToList();

        foreach (var handCard in handCards)
            await CardPileCmd.RemoveFromCombat(handCard);
    }

    public override void BetterAddExtraArgsToDescription(
        LocString description,
        PileType pileType,
        MinionLib.Utilities.BetterExtraArgs.DescriptionPreviewType previewType,
        Creature? target = null)
    {
        base.BetterAddExtraArgsToDescription(description, pileType, previewType, target);
        SyncDynamicVars();
    }

    private void SyncDynamicVars()
    {
        if (DynamicVars.TryGetValue("AccompliceStacks", out var accompliceStacks))
            accompliceStacks.BaseValue = AccompliceStacksToGain;

        if (DynamicVars.TryGetValue("AutoPlayThreshold", out var autoPlayThreshold))
            autoPlayThreshold.BaseValue = AutoPlayThreshold;

        if (DynamicVars.TryGetValue("MaxStacks", out var maxStacks))
            maxStacks.BaseValue = MaxAccompliceStacks;
    }
}
