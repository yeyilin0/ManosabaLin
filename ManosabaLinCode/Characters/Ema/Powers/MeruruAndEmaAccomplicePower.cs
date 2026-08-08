using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ema.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Combat.AttackHits;
using EmalinCharacter = ManosabaLin.Characters.Emalin.Emalin;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public sealed class MeruruAndEmaAccomplicePower : ManosabaPowerTemplate, IAttackHitHookListener
{
    public const int MaxStacks = 13;

    private const int StageEnemyMagic = 3;
    private const int StageEmaExtraDamage = 6;
    private const int StageMisdirectToEma = 9;
    private const int StageReduceOwnerToOne = 12;
    public const int PreBreakthroughMaxStacks = StageReduceOwnerToOne;
    private const int WitchificationGainOnExhaust = 50;
    private const int WitchificationLossOnDecline = 30;
    private const int MisdirectChancePercent = 13;

    private bool _isResolvingExtraDamage;

    [SavedProperty] public bool ReducedOwnerToOneThisCombat { get; set; }
    [SavedProperty] public bool DeathBreakthroughUsedThisCombat { get; set; }
    [SavedProperty] public bool ReviveUsedThisCombat { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldReceiveCombatHooks => true;
    public override int DisplayAmount => CurrentStacks;
    protected override string SmartDescriptionLocKey => $"{Id.Entry}.{SmartDescriptionKeySuffix}";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Amount", 1),
        new IntVar("MaxStacks", MaxStacks),
        new IntVar("StageEnemyMagic", StageEnemyMagic),
        new IntVar("StageEmaExtraDamage", StageEmaExtraDamage),
        new IntVar("StageMisdirectToEma", StageMisdirectToEma),
        new IntVar("StageReduceOwnerToOne", StageReduceOwnerToOne),
        new PowerVar<WithPower>("WitchificationGain", WitchificationGainOnExhaust),
        new PowerVar<WithPower>("WitchificationLoss", WitchificationLossOnDecline),
        new PowerVar<BufferPower>("Buffer", 1),
        new PowerVar<MllmPower>("Mllm", 1),
        new IntVar("MisdirectChance", MisdirectChancePercent)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<BufferPower>(),
        HoverTipFactory.FromPower<MllmPower>()
    ];

    public override LocString Description
    {
        get
        {
            var description = new LocString("powers", $"{Id.Entry}.{DescriptionKeySuffix}");
            AddDescriptionVars(description);
            return description;
        }
    }

    private int CurrentStacks => (int)Math.Clamp(Amount, 0m, MaxStacks);
    private bool IsFinalStage => CurrentStacks >= MaxStacks;

    private string DescriptionKeySuffix => CurrentStacks switch
    {
        >= MaxStacks => "description13",
        >= StageReduceOwnerToOne => "description12",
        >= StageMisdirectToEma => "description9",
        >= StageEmaExtraDamage => "description6",
        >= StageEnemyMagic => "description3",
        _ => "description"
    };

    private string SmartDescriptionKeySuffix => CurrentStacks switch
    {
        >= MaxStacks => "smartDescription13",
        >= StageReduceOwnerToOne => "smartDescription12",
        >= StageMisdirectToEma => "smartDescription9",
        >= StageEmaExtraDamage => "smartDescription6",
        >= StageEnemyMagic => "smartDescription3",
        _ => "smartDescription"
    };

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not MeruruAndEmaAccomplicePower) return false;
        if (target != Owner) return false;
        if (amount <= 0) return false;

        var remaining = Math.Max(0m, PreBreakthroughMaxStacks - Amount);
        modifiedAmount = Math.Min(amount, remaining);
        return modifiedAmount != amount;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        ClampStacks();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        if (power != this) return;
        ClampStacks();
        RefreshDynamicVars();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        var stacks = CurrentStacks;
        if (stacks <= 0) return;

        Flash();
        await ResolveRandomPlayerLayerRewards(choiceContext, player, stacks);
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (IsFinalStage) return;
        if (CurrentStacks < StageEnemyMagic) return;
        if (side == Owner.Side) return;

        Flash();
        foreach (var enemy in participants.Where(static c => c.IsAlive))
        {
            await PowerCmd.Apply<MllmPower>(
                new ThrowingPlayerChoiceContext(),
                enemy,
                1,
                Owner,
                null,
                false);
        }
    }

    public Task BeforeAttackHit(AttackHitContext context)
    {
        if (!IsFinalStage) return Task.CompletedTask;

        if (context.Dealer == Owner || context.Targets.Contains(Owner))
            context.DamageProps |= ValueProp.Unblockable | ValueProp.Unpowered;

        return Task.CompletedTask;
    }

    public async Task AfterAttackHit(AttackHitContext context)
    {
        if (_isResolvingExtraDamage) return;

        var stacks = CurrentStacks;
        if (stacks <= 0) return;

        if (IsFinalStage) return;

        if (stacks >= StageEmaExtraDamage)
            await ResolveEnemyAttackExtraEmaDamage(context, stacks);

        if (stacks >= StageMisdirectToEma)
            await ResolvePlayerAttackMisdirectToEma(context);
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature.Side == Owner.Side) return Task.CompletedTask;
        if (!creature.IsEnemy) return Task.CompletedTask;

        GrowDeckCardForEnemyDeath(1);
        return Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner) return true;

        if (IsFinalStage)
            return ReviveUsedThisCombat;

        if (TryBreakThroughToFinalStage())
            return false;

        return true;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;

        if (!IsFinalStage)
            TryBreakThroughToFinalStage();

        if (!IsFinalStage || ReviveUsedThisCombat) return;

        ReviveUsedThisCombat = true;
        Flash();
        await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);
    }

    public async Task ResolveCardPlayedStage(PlayerChoiceContext choiceContext, CardModel source)
    {
        if (CurrentStacks != StageReduceOwnerToOne) return;

        ReducedOwnerToOneThisCombat = true;
        Flash();
        if (Owner.CurrentHp > 1m)
            await CreatureCmd.SetCurrentHp(Owner, 1m);
    }

    private void ClampStacks()
    {
        if (Amount > MaxStacks)
            SetAmount(MaxStacks, silent: true);

        if (Amount > PreBreakthroughMaxStacks && !DeathBreakthroughUsedThisCombat)
            SetAmount(PreBreakthroughMaxStacks, silent: true);

        RefreshDynamicVars();
    }

    private bool TryBreakThroughToFinalStage()
    {
        if (DeathBreakthroughUsedThisCombat) return false;
        if (ReviveUsedThisCombat) return false;
        if (IsFinalStage) return false;
        if (!ReducedOwnerToOneThisCombat) return false;
        if (CurrentStacks < StageReduceOwnerToOne) return false;

        DeathBreakthroughUsedThisCombat = true;
        SetAmount(MaxStacks, silent: false);
        return true;
    }

    private void AddDescriptionVars(LocString description)
    {
        description.Add(new IntVar("Amount", CurrentStacks));
        description.Add(new IntVar("MaxStacks", MaxStacks));
        description.Add(new IntVar("StageEnemyMagic", StageEnemyMagic));
        description.Add(new IntVar("StageEmaExtraDamage", StageEmaExtraDamage));
        description.Add(new IntVar("StageMisdirectToEma", StageMisdirectToEma));
        description.Add(new IntVar("StageReduceOwnerToOne", StageReduceOwnerToOne));
        description.Add(new PowerVar<WithPower>("WitchificationGain", WitchificationGainOnExhaust));
        description.Add(new PowerVar<WithPower>("WitchificationLoss", WitchificationLossOnDecline));
        description.Add(new PowerVar<BufferPower>("Buffer", 1));
        description.Add(new PowerVar<MllmPower>("Mllm", 1));
        description.Add(new IntVar("MisdirectChance", MisdirectChancePercent));
    }

    private void RefreshDynamicVars()
    {
        if (DynamicVars.TryGetValue("Amount", out var amount))
            amount.BaseValue = CurrentStacks;
    }

    private async Task ResolveRandomPlayerLayerRewards(
        PlayerChoiceContext choiceContext,
        Player player,
        int total)
    {
        var rng = player.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < total; i++)
        {
            switch (rng.NextInt(5))
            {
                case 0:
                    await PlayerCmd.GainEnergy(1, player);
                    break;
                case 1:
                    await ResolveExhaustOrWitchificationLoss(choiceContext, player);
                    break;
                case 2:
                    await PowerCmd.Apply<BufferPower>(choiceContext, Owner, 1, Owner, null, false);
                    break;
                case 3:
                    await PowerCmd.Apply<MllmPower>(choiceContext, Owner, 1, Owner, null, false);
                    break;
                case 4:
                    await PullRandomCombatCardToHand(player);
                    break;
            }
        }
    }

    private async Task ResolveExhaustOrWitchificationLoss(PlayerChoiceContext choiceContext, Player player)
    {
        var candidates = CombatCards(player, PileType.Hand, PileType.Draw, PileType.Discard)
            .Where(static card => !card.HasBeenRemovedFromState)
            .Distinct()
            .ToList();

        CardModel? selected = null;
        if (candidates.Count > 0)
        {
            selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                player,
                new CardSelectorPrefs(
                    new LocString("powers", $"{Id.Entry}.selectionScreenPrompt"),
                    0,
                    1)
                {
                    Cancelable = true,
                    RequireManualConfirmation = false
                })).FirstOrDefault();
        }

        if (selected == null)
        {
            await PowerCmd.Apply<WithPower>(
                choiceContext,
                Owner,
                -WitchificationLossOnDecline,
                Owner,
                null,
                false);
            return;
        }

        await CardCmd.Exhaust(choiceContext, selected);
        await PowerCmd.Apply<WithPower>(
            choiceContext,
            Owner,
            WitchificationGainOnExhaust,
            Owner,
            null,
            false);
    }

    private async Task PullRandomCombatCardToHand(Player player)
    {
        var candidates = CombatCards(
                player,
                PileType.Hand,
                PileType.Draw,
                PileType.Discard,
                PileType.Exhaust)
            .Where(static card => !card.HasBeenRemovedFromState)
            .Where(static card => card is not MeruruAndEma)
            .Distinct()
            .ToArray();

        if (candidates.Length == 0) return;

        var card = player.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (card == null) return;

        card.SetFreeIgnoringCardPlayConditions();
        if (card.Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(card, PileType.Hand);
    }

    private void GrowDeckCardForEnemyDeath(int deathCount)
    {
        if (deathCount <= 0) return;

        var deckCards = Owner.Player?.Deck.Cards.OfType<MeruruAndEma>().ToList();
        if (deckCards is not { Count: > 0 }) return;

        foreach (var card in deckCards)
            card.IncreaseAccompliceStacksToGain(deathCount);

        Flash();
    }

    private async Task ResolveEnemyAttackExtraEmaDamage(AttackHitContext context, int stacks)
    {
        if (context.Dealer == null) return;
        if (context.Dealer.Side == Owner.Side) return;
        if (!context.DamageProps.IsPoweredAttack()) return;
        if (!context.Results.Any(static result => result.TotalDamage > 0m)) return;

        var emaPlayers = EmaPlayerCreatures(context.CombatState).ToArray();
        if (emaPlayers.Length == 0) return;

        _isResolvingExtraDamage = true;
        try
        {
            foreach (var ema in emaPlayers)
            {
                if (ema.CurrentHp <= 0m) continue;
                await CreatureCmd.Damage(
                    context.ChoiceContext,
                    ema,
                    stacks,
                    ValueProp.Move | ValueProp.Unpowered,
                    context.Dealer,
                    null,
                    null);
            }
        }
        finally
        {
            _isResolvingExtraDamage = false;
        }
    }

    private async Task ResolvePlayerAttackMisdirectToEma(AttackHitContext context)
    {
        if (context.Dealer == null) return;
        if (context.Dealer.Side != Owner.Side) return;
        if (!context.DamageProps.IsPoweredAttack()) return;
        if (!context.Results.Any(result => result.TotalDamage > 0m && result.Receiver.Side != Owner.Side)) return;
        if (Owner.Player is not { } ownerPlayer) return;

        var rng = ownerPlayer.RunState.Rng.CombatTargets;
        if (rng.NextFloat() >= MisdirectChancePercent / 100f) return;

        var emaPlayers = EmaPlayerCreatures(context.CombatState)
            .Where(static creature => creature.CurrentHp > 1m)
            .ToArray();
        if (emaPlayers.Length == 0) return;

        var target = rng.NextItem(emaPlayers);
        if (target == null) return;

        var amount = Math.Min(Math.Max(1m, context.Damage), target.CurrentHp - 1m);
        if (amount <= 0m) return;

        _isResolvingExtraDamage = true;
        try
        {
            await CreatureCmd.Damage(
                context.ChoiceContext,
                target,
                amount,
                ValueProp.Move | ValueProp.Unpowered,
                context.Dealer,
                null,
                null);
        }
        finally
        {
            _isResolvingExtraDamage = false;
        }
    }

    private static IEnumerable<CardModel> CombatCards(Player player, params PileType[] pileTypes)
    {
        foreach (var pileType in pileTypes)
        foreach (var card in pileType.GetPile(player).Cards)
            yield return card;
    }

    private static IEnumerable<Creature> EmaPlayerCreatures(ICombatState combatState)
    {
        return combatState.PlayerCreatures
            .Where(static creature => creature.IsAlive
                                      && creature.Player?.Character is EmalinCharacter);
    }
}
