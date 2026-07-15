using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinUnfinishedDraft()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int RequiredMagicCount = 10;
    private const string RecordedMagicKey = "RecordedMagic";
    private const string RequiredMagicKey = "RequiredMagic";

    private int _recordedMagicMask;

    private enum DraftMagic
    {
        Yalisa,
        Meruru,
        Margaret,
        Leia,
        Sherrylin,
        Hanna,
        Noah,
        Coco,
        Nayuka,
        Miria
    }

    private static readonly DraftMagic[] AllDraftMagics = Enum.GetValues<DraftMagic>();

    public override int MaxUpgradeLevel => int.MaxValue;

    [SavedProperty]
    public int RecordedMagicMask
    {
        get => _recordedMagicMask;
        set
        {
            _recordedMagicMask = value;
            RefreshRecordedMagicVar();
        }
    }

    private int RecordedMagicCount => CountRecordedMagic();
    private bool IsComplete => RecordedMagicCount >= RequiredMagicCount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new IntVar(RecordedMagicKey, RecordedMagicCount),
        new IntVar(RequiredMagicKey, RequiredMagicCount)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
         
            yield return HoverTipFactory.FromCard<AnanlinFinishedDraft>();
        }
    }

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
        if (cardPlay.Target is not { } target) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        foreach (var magic in GetRecordedMagics())
            await ApplyMagic(choiceContext, magic, target);

        if (IsComplete)
            await CreateFinishedDraft();
    }

    private async Task ApplyMagic(
        PlayerChoiceContext choiceContext,
        DraftMagic magic,
        Creature target)
    {
        switch (magic)
        {
            case DraftMagic.Yalisa:
                await PowerCmd.Apply<YlsmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Meruru:
                await PowerCmd.Apply<MllmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Margaret:
                await PowerCmd.Apply<MgmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Leia:
                var lym = await PowerCmd.Apply<LymPower>(choiceContext, target, 1m, Owner.Creature, this);
                if (lym is not null)
                    await lym.HandleGameAction(target);
                break;
            case DraftMagic.Sherrylin:
                await PowerCmd.Apply<XlmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Hanna:
                await PowerCmd.Apply<HnmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Noah:
                await PowerCmd.Apply<NymPower>(choiceContext, target, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Coco:
                await PowerCmd.Apply<KkmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Nayuka:
                await PowerCmd.Apply<NyxmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
            case DraftMagic.Miria:
                await PowerCmd.Apply<MlyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
                break;
        }
    }

    private async Task CreateFinishedDraft()
    {
        if (Owner.Creature.CombatState is not { } combatState) return;

        var finished = combatState.CreateCard<AnanlinFinishedDraft>(Owner);
        finished.InheritedUpgradeLevel = Math.Max(0, CurrentUpgradeLevel - RequiredMagicCount);
        await CardPileCmd.AddGeneratedCardToCombat(finished, PileType.Hand, Owner);
    }

    private IEnumerable<DraftMagic> GetRecordedMagics()
    {
        return AllDraftMagics.Where(HasMagic);
    }

    private bool HasMagic(DraftMagic magic)
    {
        return (RecordedMagicMask & (1 << (int)magic)) != 0;
    }

    private int CountRecordedMagic()
    {
        var count = 0;
        foreach (var magic in AllDraftMagics)
            if (HasMagic(magic))
                count++;

        return count;
    }

    private void RecordRandomNewMagic()
    {
        if (Owner == null) return;

        var candidates = AllDraftMagics
            .Where(magic => !HasMagic(magic))
            .ToArray();
        if (candidates.Length == 0) return;

        var rng = Owner.Creature.CombatState is null
            ? Owner.RunState.Rng.UpFront
            : Owner.RunState.Rng.CombatCardGeneration;
        var selected = candidates[rng.NextInt(candidates.Length)];
        RecordedMagicMask |= 1 << (int)selected;
        RefreshRecordedMagicVar();
    }

    private void RefreshRecordedMagicVar()
    {
        if (DynamicVars.TryGetValue(RecordedMagicKey, out var recordedMagicVar))
            recordedMagicVar.BaseValue = RecordedMagicCount;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        if (Owner != null)
            RecordRandomNewMagic();
    }
}
