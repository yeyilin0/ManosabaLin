using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Rooms;
using ManosabaLin.Characters.Ananlin.Vfx;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinUnfinishedDraft()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int RequiredMagicCount = 10;
    private const int PlaysPerPermanentUpgrade = 3;
    private const string RecordedMagicKey = "RecordedMagic";
    private const string RequiredMagicKey = "RequiredMagic";
    private const string PlayProgressKey = "PlayProgress";
    private const string PlayRequirementKey = "PlayRequirement";

    private int _recordedMagicMask;
    private int _playProgress;
    private bool _suppressMagicRecordOnNextUpgrade;

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

    // The completed card is generated during combat, so prepare its theater from
    // the source card rather than waiting for the player's first completed play.
    protected override IEnumerable<string> ExtraRunAssetPaths =>
        base.ExtraRunAssetPaths.Concat(FinishedDraftVfx.AssetPaths);

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

    [SavedProperty]
    public int PlayProgress
    {
        get => _playProgress;
        set
        {
            _playProgress = Math.Max(0, value);
            RefreshPlayProgressVar();
        }
    }

    private int RecordedMagicCount => CountRecordedMagic();
    private bool IsComplete => RecordedMagicCount >= RequiredMagicCount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new IntVar(RecordedMagicKey, RecordedMagicCount),
        new IntVar(RequiredMagicKey, RequiredMagicCount),
        new IntVar(PlayProgressKey, PlayProgress),
        new IntVar(PlayRequirementKey, PlaysPerPermanentUpgrade)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
         
            yield return HoverTipFactory.FromCard<AnanlinFinishedDraft>();
        }
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        return IsComplete
            ? new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlayC();
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

        var wasComplete = IsComplete;
        AdvancePermanentUpgradeProgress();
        // 若本次打出的永久升级补上了最后一种魔法，立即生成【完稿】
        if (wasComplete || IsComplete)
        {
            await CreateFinishedDraft();
            // The game captured resultLocation before OnPlay. Explicitly exhaust
            // after completion, including the play which just records the tenth magic.
            // Moving out of Play also prevents the wrapper from discarding it later.
            if (Pile?.Type is not PileType.Exhaust and not PileType.None && Pile != null)
                await CardCmd.Exhaust(choiceContext, this);
        }
        else
            await PlayDraftButterflies(null);
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
        await PlayDraftButterflies(finished);
        await CardPileCmd.AddGeneratedCardToCombat(finished, PileType.Hand, Owner);
    }

    private async Task PlayDraftButterflies(CardModel? finished)
    {
        var visuals = GetRecordedMagics().Select(magic =>
        {
            PowerModel power = magic switch
            {
                DraftMagic.Yalisa => ModelDb.Power<YlsmPower>(),
                DraftMagic.Meruru => ModelDb.Power<MllmPower>(),
                DraftMagic.Margaret => ModelDb.Power<MgmPower>(),
                DraftMagic.Leia => ModelDb.Power<LymPower>(),
                DraftMagic.Sherrylin => ModelDb.Power<XlmPower>(),
                DraftMagic.Hanna => ModelDb.Power<HnmPower>(),
                DraftMagic.Noah => ModelDb.Power<NymPower>(),
                DraftMagic.Coco => ModelDb.Power<KkmPower>(),
                DraftMagic.Nayuka => ModelDb.Power<NyxmPower>(),
                DraftMagic.Miria => ModelDb.Power<MlyPower>(),
                _ => throw new ArgumentOutOfRangeException(nameof(magic))
            };
            var fallback = magic == DraftMagic.Noah ? "MANOSABA_LIN_CARD_ANANLIN_NYM.description" : null;
            return new DraftButterflyMagic((int)magic, DraftButterflyVfx.ReadMagicColor(power, fallback));
        }).ToArray();
        using var butterflies = DraftButterflyVfx.Play(Owner.Creature, visuals, finished);
        if (butterflies != null)
            await butterflies.Finished;
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

    private void RefreshPlayProgressVar()
    {
        if (DynamicVars.TryGetValue(PlayProgressKey, out var playProgressVar))
            playProgressVar.BaseValue = PlayProgress;
    }

    private void AdvancePermanentUpgradeProgress()
    {
        var deckVersion = FindDeckVersion();
        var progressCard = deckVersion ?? this;
        progressCard.PlayProgress++;

        if (progressCard.PlayProgress >= PlaysPerPermanentUpgrade)
        {
            progressCard.PlayProgress = 0;
            PermanentlyUpgradeSelf(deckVersion);
        }

        // 把进度同步给同一张牌组卡的所有战斗内副本，保证每张未完稿都显示一致
        SyncPlayProgressDisplay(progressCard);
    }

    /// <summary>
    /// 找到本卡对应的牌组卡：优先用 DeckVersion；生成卡没有 DeckVersion 时，
    /// 回退到玩家牌组中同名的未完稿，保证「牌组里面的卡牌同样升级一次」。
    /// </summary>
    private AnanlinUnfinishedDraft? FindDeckVersion()
    {
        if (DeckVersion is AnanlinUnfinishedDraft deckVersion)
            return deckVersion;

        return Owner?.Deck.Cards.OfType<AnanlinUnfinishedDraft>().FirstOrDefault();
    }

    private void SyncPlayProgressDisplay(AnanlinUnfinishedDraft progressCard)
    {
        if (Owner?.PlayerCombatState == null)
            return;

        foreach (var copy in Owner.PlayerCombatState.AllCards.OfType<AnanlinUnfinishedDraft>())
        {
            // 只同步与本次进度同源的副本：同一张牌组卡的所有克隆，或生成卡自身
            if (copy.DeckVersion == progressCard || ReferenceEquals(copy, progressCard))
                copy.PlayProgress = progressCard.PlayProgress;
        }
    }

    private void PermanentlyUpgradeSelf(AnanlinUnfinishedDraft? deckVersion)
    {
        if (deckVersion is not null)
        {
            if (!deckVersion.IsUpgradable)
                return;

            // 牌组卡升级：+1 伤害并记录 1 种尚未记录的魔法
            CardCmd.Upgrade(deckVersion, CardPreviewStyle.None);

            // 战斗内本卡同步升级（不再重复记录魔法），并把牌组已记录的魔法同步回来
            if (!IsUpgradable)
                return;

            _suppressMagicRecordOnNextUpgrade = true;
            try
            {
                UpgradeInternal();
                FinalizeUpgradeInternal();
            }
            finally
            {
                _suppressMagicRecordOnNextUpgrade = false;
            }

            RecordedMagicMask = deckVersion.RecordedMagicMask;
        }
        else
        {
            // 没有牌组卡（纯生成）：只升级战斗内本卡，正常记录 1 种新魔法
            CardCmd.Upgrade(this, CardPreviewStyle.None);
        }

        // 刷新本卡卡面视觉（正在打出/位于手牌中的卡不会自动刷新升级后的数值）
        if (NCard.FindOnTable(this) is { } node)
        {
            node.UpdateVisuals(Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        if (!_suppressMagicRecordOnNextUpgrade && Owner != null)
            RecordRandomNewMagic();
    }
}
