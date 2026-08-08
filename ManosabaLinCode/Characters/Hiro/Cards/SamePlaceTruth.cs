using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.Component.Core;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class SamePlaceTruth()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Ancient, TargetType.Self), IEasyRightClickableCard
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_SAME_PLACE_TRUTH_EFFECT";
    private const string AutoPlayUnlockProgressKey = "AutoPlayUnlockProgress";
    private const string AutoPlayUnlockRequirementKey = "AutoPlayUnlockRequirement";
    private const int AutoPlayUnlockRequirement = 13;

    private bool _lockedInDiscard;
    private int _autoPlayUnlockProgress;
    private bool _movingFromLockRelease;
    private bool _enforcingDiscardLock;

    [SavedProperty]
    public bool LockedInDiscard
    {
        get => _lockedInDiscard;
        private set
        {
            _lockedInDiscard = value;
            RefreshLockVars();
        }
    }

    [SavedProperty]
    public int AutoPlayUnlockProgress
    {
        get => _autoPlayUnlockProgress;
        private set
        {
            _autoPlayUnlockProgress = Math.Clamp(value, 0, AutoPlayUnlockRequirement);
            RefreshLockVars();
        }
    }

    public static bool IsSelectionLocked(CardModel? card)
    {
        return card is SamePlaceTruth { LockedInDiscard: true };
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new IntVar(AutoPlayUnlockProgressKey, AutoPlayUnlockProgress);
            yield return new IntVar(AutoPlayUnlockRequirementKey, AutoPlayUnlockRequirement);
        }
    }

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        return Pile?.Type == PileType.Hand && !SamePlaceTruthFusionState.IsQueued(this);
    }

    public Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (Pile?.Type != PileType.Hand)
        {
            return Task.CompletedTask;
        }

        SamePlaceTruthFusionState.Queue(this);
        RefreshCardVisuals();
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (SamePlaceTruthFusionState.Consume(this))
        {
            await PlayPendingTruthEffect(choiceContext, cardPlay, componentContext);
        }

        LockInDiscard();
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        return new CardLocation(Owner, PileType.Discard, CardPilePosition.Bottom);
    }

    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw,
        ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this))
        {
            await EnforceDiscardLock();
        }
    }

    protected override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this))
        {
            await EnforceDiscardLock();
        }
    }

    internal static async Task AdvanceLockedTruthsForAutoPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!cardPlay.IsAutoPlay || !TransmigrationRules.HasTransmigration(cardPlay.Card))
        {
            return;
        }

        var owner = cardPlay.Card.Owner;
        if (owner?.PlayerCombatState == null)
        {
            return;
        }

        var lockedTruths = owner.PlayerCombatState.AllCards
            .OfType<SamePlaceTruth>()
            .Where(static truth => truth.LockedInDiscard)
            .ToArray();

        foreach (var truth in lockedTruths)
        {
            await truth.AdvanceAutoPlayUnlockProgress(choiceContext);
        }
    }

    private static Task PlayPendingTruthEffect(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }

    private void LockInDiscard()
    {
        LockedInDiscard = true;
        AutoPlayUnlockProgress = 0;
        RefreshCardVisuals();
    }

    private async Task AdvanceAutoPlayUnlockProgress(PlayerChoiceContext choiceContext)
    {
        if (!LockedInDiscard)
        {
            return;
        }

        AutoPlayUnlockProgress++;
        RefreshCardVisuals();

        if (AutoPlayUnlockProgress < AutoPlayUnlockRequirement)
        {
            return;
        }

        await ReleaseFromDiscardLock();
    }

    private async Task ReleaseFromDiscardLock()
    {
        LockedInDiscard = false;
        AutoPlayUnlockProgress = 0;
        _movingFromLockRelease = true;
        try
        {
            await CardPileCmd.Add(this, PileType.Hand, CardPilePosition.Bottom);
        }
        finally
        {
            _movingFromLockRelease = false;
        }

        RefreshCardVisuals();
    }

    private async Task EnforceDiscardLock()
    {
        if (!LockedInDiscard || _movingFromLockRelease || _enforcingDiscardLock || Pile?.Type == PileType.Discard)
        {
            return;
        }

        _enforcingDiscardLock = true;
        try
        {
            await CardPileCmd.Add(this, PileType.Discard, CardPilePosition.Bottom, skipVisuals: true);
        }
        finally
        {
            _enforcingDiscardLock = false;
        }

        RefreshCardVisuals();
    }

    private void RefreshLockVars()
    {
        if (DynamicVars.TryGetValue(AutoPlayUnlockProgressKey, out var progressVar))
        {
            progressVar.BaseValue = AutoPlayUnlockProgress;
        }

        if (DynamicVars.TryGetValue(AutoPlayUnlockRequirementKey, out var requirementVar))
        {
            requirementVar.BaseValue = AutoPlayUnlockRequirement;
        }
    }

    private void RefreshCardVisuals()
    {
        var node = NCard.FindOnTable(this);
        node?.UpdateVisuals(Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }
}

internal static class SamePlaceTruthFusionState
{
    private static readonly HashSet<CardModel> QueuedCardRefs = new(ReferenceEqualityComparer.Instance);
    private static readonly HashSet<NetCombatCard> QueuedCombatCards = [];

    public static bool IsQueued(CardModel card)
    {
        return QueuedCardRefs.Contains(card) || QueuedCombatCards.Contains(ToKey(card));
    }

    public static void Queue(CardModel card)
    {
        QueuedCardRefs.Add(card);
        QueuedCombatCards.Add(ToKey(card));
    }

    public static bool Consume(CardModel card)
    {
        var removedRef = QueuedCardRefs.Remove(card);
        var removedKey = QueuedCombatCards.Remove(ToKey(card));
        return removedRef || removedKey;
    }

    public static void Reset(CardModel card)
    {
        QueuedCardRefs.Remove(card);
        QueuedCombatCards.Remove(ToKey(card));
    }

    private static NetCombatCard ToKey(CardModel card)
    {
        return NetCombatCard.FromModel(card);
    }
}
