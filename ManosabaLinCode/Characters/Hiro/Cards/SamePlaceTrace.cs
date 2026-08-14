using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Capabilities;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class SamePlaceTrace() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string CopyCountKey = "CopyCount";
    private const string AutoPlayProgressKey = "AutoPlayProgress";
    private const string AutoPlayRequirementKey = "AutoPlayRequirement";
    private const string ClueCountKey = "ClueCount";
    private const string RequiredCluesKey = "RequiredClues";
    private const int AutoPlayRequirement = 3;
    private const int RequiredClues = 3;

    private int _autoPlayProgress;
    private int _clueCount;
    private bool _completionTriggered;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return TransmigrationRules.TransmigrationCardKeyword;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SamePlaceTruth>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new IntVar(CopyCountKey, 1);
            yield return new CardsVar(1);
            yield return new EnergyVar(1);
            yield return new IntVar(AutoPlayProgressKey, _autoPlayProgress);
            yield return new IntVar(AutoPlayRequirementKey, AutoPlayRequirement);
            yield return new IntVar(ClueCountKey, _clueCount);
            yield return new IntVar(RequiredCluesKey, RequiredClues);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await AddTemporaryCopiesToDrawPile();
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        if (!cardPlay.IsAutoPlay)
        {
            return;
        }

        await GrantTransmigrationToHandCard(choiceContext);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await AdvanceAutoPlayProgress(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[CopyCountKey].BaseValue++;
    }

    private async Task AddTemporaryCopiesToDrawPile()
    {
        var progress = SnapshotProgress();
        SetProgressForAll(progress.AutoPlayProgress, progress.ClueCount, progress.CompletionTriggered);

        for (var i = 0; i < DynamicVars[CopyCountKey].IntValue; i++)
        {
            var copy = CombatState.CreateCard<SamePlaceTrace>(Owner);
            for (var upgradeLevel = 0; upgradeLevel < CurrentUpgradeLevel; upgradeLevel++)
            {
                copy.UpgradeInternal();
            }

            copy.SetProgressLocal(progress.AutoPlayProgress, progress.ClueCount, progress.CompletionTriggered);
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }

    private async Task GrantTransmigrationToHandCard(PlayerChoiceContext choiceContext)
    {
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
            card => !ReferenceEquals(card, this) && !TransmigrationRules.HasTransmigration(card),
            this);

        foreach (var selectedCard in selectedCards)
        {
            selectedCard.AddModKeyword(TransmigrationRules.TransmigrationCardKeyword);
            // 真相能力：给予卡牌轮回关键词时自动追加真相组件
            if (TruthPower.HasTruthPower(Owner))
            {
                selectedCard.GetOrCreateCapability<TruthComponentCapability>();
            }
        }
    }

    private async Task AdvanceAutoPlayProgress(PlayerChoiceContext choiceContext)
    {
        var progress = SnapshotProgress();
        if (progress.CompletionTriggered)
        {
            SetProgressForAll(progress.AutoPlayProgress, progress.ClueCount, true);
            return;
        }

        var autoPlayProgress = progress.AutoPlayProgress + 1;
        var clueCount = progress.ClueCount;

        if (autoPlayProgress >= AutoPlayRequirement)
        {
            autoPlayProgress -= AutoPlayRequirement;
            clueCount++;
        }

        var completed = clueCount >= RequiredClues;
        SetProgressForAll(autoPlayProgress, clueCount, completed);
        if (!completed)
        {
            return;
        }

        await ExhaustAllSamePlaceTrace(choiceContext);
        await GainAncientPlaceholder();
    }

    private async Task ExhaustAllSamePlaceTrace(PlayerChoiceContext choiceContext)
    {
        foreach (var trace in GetAllSamePlaceTraceCards()
                     .Where(card => card.Pile?.Type is PileType.Hand or PileType.Draw or PileType.Discard)
                     .ToList())
        {
            await CardCmd.Exhaust(choiceContext, trace);
        }

        ExhaustOnNextPlay = true;
        AddKeyword(CardKeyword.Exhaust);
    }

    private async Task GainAncientPlaceholder()
    {
        var ancientCard = CombatState.CreateCard<SamePlaceTruth>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(ancientCard, PileType.Hand, Owner);
    }

    private (int AutoPlayProgress, int ClueCount, bool CompletionTriggered) SnapshotProgress()
    {
        var traceCards = GetAllSamePlaceTraceCards();
        if (traceCards.Count == 0)
        {
            return (_autoPlayProgress, _clueCount, _completionTriggered);
        }

        return (
            traceCards.Max(card => card._autoPlayProgress),
            traceCards.Max(card => card._clueCount),
            traceCards.Any(card => card._completionTriggered));
    }

    private void SetProgressForAll(int autoPlayProgress, int clueCount, bool completionTriggered)
    {
        foreach (var trace in GetAllSamePlaceTraceCards())
        {
            trace.SetProgressLocal(autoPlayProgress, clueCount, completionTriggered);
        }
    }

    private void SetProgressLocal(int autoPlayProgress, int clueCount, bool completionTriggered)
    {
        _autoPlayProgress = autoPlayProgress;
        _clueCount = clueCount;
        _completionTriggered = completionTriggered;
        RefreshProgressVars();
    }

    private void RefreshProgressVars()
    {
        DynamicVars[AutoPlayProgressKey].BaseValue = _autoPlayProgress;
        DynamicVars[ClueCountKey].BaseValue = _clueCount;
    }

    private List<SamePlaceTrace> GetAllSamePlaceTraceCards()
    {
        var traceCards = Owner.PlayerCombatState.AllCards.OfType<SamePlaceTrace>().ToList();
        if (!traceCards.Contains(this))
        {
            traceCards.Add(this);
        }

        return traceCards;
    }
}
