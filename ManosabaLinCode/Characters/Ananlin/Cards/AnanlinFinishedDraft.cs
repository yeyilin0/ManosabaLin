using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using ManosabaLin.Characters.Ananlin.Vfx;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class AnanlinFinishedDraft()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
{
    private const string InheritedKey = "Inherited";

    private int _inheritedUpgradeLevel;

    protected override IEnumerable<string> ExtraRunAssetPaths =>
        base.ExtraRunAssetPaths.Concat(FinishedDraftVfx.AssetPaths);

    public override int MaxUpgradeLevel => 0;

    [SavedProperty]
    public int InheritedUpgradeLevel
    {
        get => _inheritedUpgradeLevel;
        set
        {
            _inheritedUpgradeLevel = Math.Max(0, value);
            RefreshInheritedVar();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new IntVar(InheritedKey, InheritedUpgradeLevel),
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar("Vigor").WithMultiplier((card, _) => ((AnanlinFinishedDraft)card).CalculateVigorAmount()),
        new CalculatedVar("Hits").WithMultiplier((card, _) => ((AnanlinFinishedDraft)card).CalculateHitCount())
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        using var theater = FinishedDraftVfx.Play(Owner.Creature, cardPlay.Target);
        if (theater != null) await theater.Opened;
        try
        {
        var vigorAmount = CalculateVigorAmount();
        if (vigorAmount > 0)
        {
            await PowerCmd.Apply<VigorPower>(
                choiceContext,
                Owner.Creature,
                vigorAmount,
                Owner.Creature,
                this);
        }

        var hitCount = CalculateHitCount();
        if (hitCount <= 0 || cardPlay.Target is not { } target) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitCount(hitCount)
            .BeforeDamage(() => theater?.Hit() ?? Task.CompletedTask)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
        }
        finally
        {
            if (theater != null) { theater.End(); await theater.Closed; }
        }
    }

    private int CalculateVigorAmount()
    {
        return CountOtherCardPoolCardsPlayedThisCombat() + Math.Max(0, InheritedUpgradeLevel);
    }

    protected override Task BeforeCombatStart(ComponentContext componentContext)
    {
        FinishedDraftVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task AfterRoomEntered(AbstractRoom room, ComponentContext componentContext)
    {
        if (room is CombatRoom) FinishedDraftVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task AfterCardEnteredCombat(CardModel card, ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this)) FinishedDraftVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    private int CalculateHitCount()
    {
        return AnanlinSilenceIntentManager.GetRewritesThisCombat(Owner) + Math.Max(0, InheritedUpgradeLevel);
    }

    private int CountOtherCardPoolCardsPlayedThisCombat()
    {
        return CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
            entry.CardPlay.Card.Owner == Owner
            && entry.CardPlay.Card != this
            && entry.CardPlay.Card.Pool is not AnanlinCardPool);
    }

    private void RefreshInheritedVar()
    {
        if (DynamicVars.TryGetValue(InheritedKey, out var inheritedVar))
            inheritedVar.BaseValue = InheritedUpgradeLevel;
    }
}
