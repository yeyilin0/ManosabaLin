using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Compat.Core;
using MegaCrit.Sts2.Core.Hooks;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Cards;

internal static class AnanlinCardHelpers
{
    private const int FallbackGeneratedHitCount = 6;
    private const string OstyDamageKey = "OstyDamage";
    private const string CalculationBaseKey = "CalculationBase";

    private static readonly HashSet<string> StableMultiHitAttackNames =
    [
        "AstralPulse",
        "CelestialMight",
        "Conflagration",
        "DaggerSpray",
        "Dismantle",
        "Exterminate",
        "FightMe",
        "GunkUp",
        "Maul",
        "Omnislice",
        "OneTwoPunch",
        "Peck",
        "Quadcast",
        "Refract",
        "Ricochet",
        "RipAndTear",
        "SevenStars",
        "SwordBoomerang",
        "Thrash",
        "TwinStrike",
        "Uproar",
        "Volley"
    ];

    private static readonly string[] MultiHitVarNames =
    [
        "HitCount",
        "Hits",
        "CalculatedHits",
        "Repeat",
        "Repeats",
        "AttackCount",
        "ShotCount",
        "Times"
    ];

    internal static AnansSketchbook? Sketchbook(this CardModel card)
    {
        return card.Owner.Relics.OfType<AnansSketchbook>().FirstOrDefault();
    }

    internal static async Task AddBlankPageToHand(this CardModel source, bool upgraded)
    {
        var page = source.CreateBlankPageOrBlessedReplacement(upgraded);
        await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Hand, source.Owner);
    }

    internal static async Task AddBlankPageToDrawPile(this CardModel source, bool upgraded)
    {
        var page = source.CreateBlankPageOrBlessedReplacement(upgraded);
        await CardPileCmd.AddGeneratedCardToCombat(page, PileType.Draw, source.Owner, CardPilePosition.Random);
    }

    internal static CardModel CreateBlankPageOrBlessedReplacement(this CardModel source, bool upgraded)
    {
        var combatState = source.CombatState ?? source.Owner.Creature.CombatState;
        ArgumentNullException.ThrowIfNull(combatState, nameof(source.CombatState));

        var blessedObject = source.Owner.Relics.OfType<BlessedObject>().FirstOrDefault();
        if (blessedObject is not null)
            return blessedObject.CreateBlankPageOrReplacement(combatState, upgraded, source.Owner);

        var blankPage = combatState.CreateCard<BlankPage>(source.Owner);
        if (upgraded)
            CardCmd.Upgrade(blankPage);

        return blankPage;
    }

    internal static async Task AddMarginPageToHand(this CardModel source, bool upgraded)
    {
        var marginPage = source.CombatState.CreateCard<MarginPage>(source.Owner);
        if (upgraded)
            CardCmd.Upgrade(marginPage);

        await CardPileCmd.AddGeneratedCardToCombat(marginPage, PileType.Hand, source.Owner);
    }

    internal static int PeaceOfMindAmount(this CardModel card)
    {
        return Math.Max(0, (int)(card.Owner.Creature.GetPower<AnanlinPeaceOfMindPower>()?.Amount ?? 0));
    }

    internal static bool HasLostPeaceOfMindThisTurn(this CardModel card)
    {
        return card.Sketchbook()?.PeaceLostThisTurn == true;
    }

    internal static async Task GainPeaceOfMind(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount = 1)
    {
        if (amount <= 0) return;

        await PowerCmd.Apply<AnanlinPeaceOfMindPower>(
            choiceContext,
            card.Owner.Creature,
            amount,
            card.Owner.Creature,
            card);
    }

    internal static Task AddSilence(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount)
    {
        return card.Sketchbook() is { } sketchbook
            ? sketchbook.AddSilence(choiceContext, amount, card)
            : PowerCmd.Apply<SilentPower>(
                choiceContext,
                card.Owner.Creature,
                amount,
                card.Owner.Creature,
                card);
    }

    internal static async Task<int> LosePeaceOfMind(
        this CardModel card,
        PlayerChoiceContext choiceContext,
        int amount = 1)
    {
        if (amount <= 0) return 0;
        var peace = card.Owner.Creature.GetPower<AnanlinPeaceOfMindPower>();
        if (peace is null || peace.Amount <= 0) return 0;

        var lost = Math.Min(amount, (int)peace.Amount);
        await PowerCmd.ModifyAmount(choiceContext, peace, -lost, card.Owner.Creature, card);

        // 一次性失去 2 层及以上安心：选择一张手牌获得【重放1】
        if (lost >= 2)
            await GrantReplayForPeaceLoss(choiceContext, card.Owner);

        return lost;
    }

    private static async Task GrantReplayForPeaceLoss(PlayerChoiceContext choiceContext, Player player)
    {
        var hand = PileType.Hand.GetPile(player).Cards
            .Where(IsPlayableCombatCard)
            .ToArray();
        if (hand.Length == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            hand,
            player,
            new CardSelectorPrefs(
                new LocString("powers", "MANOSABA_LIN_POWER_ANANLIN_PEACE_OF_MIND_POWER.selectionScreenPrompt"),
                1,
                1))).FirstOrDefault();
        if (selected is null) return;

        selected.BaseReplayCount++;
    }

    internal static async Task<int> PullMatchingCardsToHand(
        this CardModel source,
        PlayerChoiceContext choiceContext,
        int count,
        Func<CardModel, bool> predicate)
    {
        if (count <= 0) return 0;

        var added = 0;
        for (var i = 0; i < count; i++)
        {
            var card = FindMatchingCard(source.Owner, predicate);
            if (card is null) break;

            await CardPileCmd.Add(card, PileType.Hand);
            added++;
        }

        return added;
    }

    internal static bool IsPlayableCombatCard(CardModel card)
    {
        return card.Rarity is not CardRarity.Status
            and not CardRarity.Curse
            and not CardRarity.Quest
            and not CardRarity.Event;
    }

    internal static bool IsStatusOrCurse(CardModel card)
    {
        return card.Rarity is CardRarity.Status or CardRarity.Curse
            || card.Type is CardType.Status or CardType.Curse;
    }

    internal static bool IsStatus(CardModel card)
    {
        return card.Rarity == CardRarity.Status || card.Type == CardType.Status;
    }

    internal static bool IsAnanlinPoolCard(CardModel card)
    {
        return card.Pool.Id == ModelDb.GetId(typeof(AnanlinCardPool));
    }

    internal static void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        for (var i = 0; i < source.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(target);
    }

    internal static void SetFreeIgnoringCardPlayConditions(this CardModel card)
    {
        card.SetToFreeThisTurn();
        card.GetOrCreateCapability<AnanlinIgnorePlayConditionsCapability>();
    }

    internal static CardModel? RollPlayableStableMultiHitAttack(
        Player player,
        ICombatState combatState,
        bool setBaseCostToZero)
    {
        var candidates = BuildPlayableStableMultiHitAttacks(player, combatState, setBaseCostToZero).ToArray();
        return candidates.Length == 0
            ? null
            : player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
    }

    internal static IEnumerable<CardModel> BuildPlayableStableMultiHitAttacks(
        Player player,
        ICombatState combatState,
        bool setBaseCostToZero)
    {
        var seenIds = new HashSet<ModelId>();

        foreach (var template in ModelDb.AllCards)
        {
            if (!seenIds.Add(template.Id)) continue;
            if (!CanUseCardLibraryCandidate(template, player)) continue;
            if (IsOstyAttack(template)) continue;
            if (!IsStableMultiHitAttack(template)) continue;

            var card = combatState.CreateCard(template, player);
            if (setBaseCostToZero && !card.EnergyCost.CostsX && card.EnergyCost.Canonical > 0)
                card.EnergyCost.UpgradeBy(-card.EnergyCost.Canonical);

            SetZeroHitCountToFallback(card);
            card.SetFreeIgnoringCardPlayConditions();

            if (HasValidEffectTarget(card, combatState))
                yield return card;
        }
    }

    internal static async Task ResolveAsFreeCardEffect(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target = null,
        bool skipCardPileVisuals = true)
    {
        if (CombatManager.Instance.IsOverOrEnding || card.Owner.Creature.IsDead) return;

        var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        if (combatState is null) return;
        if (!HasValidEffectTarget(card, combatState)) return;

        var resolvedTarget = PickValidEffectTarget(card, combatState, target);
        if (RequiresExplicitEffectTarget(card.TargetType) && resolvedTarget is null) return;

        if (card.EnergyCost.CostsX)
            card.EnergyCost.CapturedXValue = 0;
        if (card.HasStarCostX)
            card.LastStarsSpent = 0;

        card.GetOrCreateCapability<AnanlinResolveOnlyCapability>();
        await Hook.BeforeCardAutoPlayed(combatState, card, resolvedTarget, AutoPlayType.Default);
        await card.OnPlayWrapper(
            choiceContext,
            resolvedTarget,
            isAutoPlay: true,
            new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = 0,
                StarsSpent = 0,
                StarValue = 0
            },
            skipCardPileVisuals);

        if (card.Pile?.IsCombatPile == true)
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
    }

    internal static bool HasValidEffectTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.HittableEnemies.Any(card.IsValidTarget),
            TargetType.RandomEnemy or TargetType.AllEnemies => combatState.HittableEnemies.Any(),
            TargetType.AnyAlly => ValidAllyTargets(card, combatState).Any(),
            TargetType.AnyPlayer => ValidPlayerTargets(card, combatState).Any(),
            _ => card.IsValidTarget(null)
        };
    }

    private static bool CanUseCardLibraryCandidate(CardModel template, Player player)
    {
        if (!template.ShouldShowInCardLibrary) return false;
        if (!CompatContentGate.IsCompendiumCardVisible(template)) return false;
        if (!template.CanBeGeneratedInCombat) return false;
        if (!IsPlayableCombatCard(template)) return false;
        if (template.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly
            && player.RunState.CardMultiplayerConstraint == CardMultiplayerConstraint.SingleplayerOnly)
            return false;
        if (template.MultiplayerConstraint == CardMultiplayerConstraint.SingleplayerOnly
            && player.RunState.CardMultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly)
            return false;

        return true;
    }

    private static bool IsStableMultiHitAttack(CardModel template)
    {
        if (template.Type != CardType.Attack) return false;
        if (template.EnergyCost.CostsX) return false;
        if (!HasDamageOutput(template)) return false;
        if (template.DynamicVars.ContainsKey("CardCount")) return false;

        foreach (var varName in MultiHitVarNames)
        {
            if (template.DynamicVars.TryGetValue(varName, out var dynamicVar) && IsMultiHitVar(dynamicVar))
                return true;
        }

        var title = template.Title;
        return StableMultiHitAttackNames.Contains(template.GetType().Name)
            || StableMultiHitAttackNames.Contains(template.Id.Entry)
            || StableMultiHitAttackNames.Contains(title);
    }

    private static bool HasDamageOutput(CardModel template)
    {
        return template.DynamicVars.ContainsKey("Damage")
            || template.DynamicVars.ContainsKey("CalculatedDamage");
    }

    private static bool IsMultiHitVar(DynamicVar dynamicVar)
    {
        if (dynamicVar is CalculatedVar)
            return true;

        return dynamicVar.IntValue > 1;
    }

    private static bool IsOstyAttack(CardModel template)
    {
        return template.Type == CardType.Attack && template.DynamicVars.ContainsKey(OstyDamageKey);
    }

    private static void SetZeroHitCountToFallback(CardModel card)
    {
        foreach (var varName in MultiHitVarNames)
        {
            if (!card.DynamicVars.TryGetValue(varName, out var dynamicVar)) continue;
            if (GetHitCountValue(dynamicVar) != 0) return;

            if (dynamicVar is CalculatedVar
                && card.DynamicVars.TryGetValue(CalculationBaseKey, out var calculationBase))
            {
                calculationBase.BaseValue = FallbackGeneratedHitCount;
            }
            else
            {
                dynamicVar.BaseValue = FallbackGeneratedHitCount;
            }

            return;
        }
    }

    private static int GetHitCountValue(DynamicVar dynamicVar)
    {
        return dynamicVar is CalculatedVar calculatedVar
            ? (int)calculatedVar.Calculate(null)
            : dynamicVar.IntValue;
    }

    internal static Creature? PickValidEffectTarget(
        CardModel card,
        ICombatState combatState,
        Creature? requestedTarget = null)
    {
        if (requestedTarget is not null && card.IsValidTarget(requestedTarget))
            return requestedTarget;

        return card.TargetType switch
        {
            TargetType.AnyEnemy => PickRandomTarget(card, combatState.HittableEnemies.Where(card.IsValidTarget)),
            TargetType.AnyAlly => PickRandomTarget(card, ValidAllyTargets(card, combatState)),
            TargetType.AnyPlayer => PickRandomTarget(card, ValidPlayerTargets(card, combatState)),
            _ => null
        };
    }

    private static CardModel? FindMatchingCard(Player player, Func<CardModel, bool> predicate)
    {
        var hand = PileType.Hand.GetPile(player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand) return null;

        var candidates = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Where(static card => !SamePlaceTruth.IsSelectionLocked(card))
            .Where(predicate)
            .ToArray();
        return candidates.Length == 0
            ? null
            : player.RunState.Rng.CombatCardSelection.NextItem(candidates);
    }

    private static bool RequiresExplicitEffectTarget(TargetType targetType)
    {
        return targetType is TargetType.AnyEnemy or TargetType.AnyAlly or TargetType.AnyPlayer;
    }

    private static IEnumerable<Creature> ValidAllyTargets(CardModel card, ICombatState combatState)
    {
        return combatState.PlayerCreatures
            .Where(creature => creature.IsAlive
                && creature.IsPlayer
                && creature != card.Owner.Creature
                && card.IsValidTarget(creature));
    }

    private static IEnumerable<Creature> ValidPlayerTargets(CardModel card, ICombatState combatState)
    {
        return combatState.PlayerCreatures
            .Where(creature => creature.IsAlive
                && creature.IsPlayer
                && card.IsValidTarget(creature));
    }

    private static Creature? PickRandomTarget(CardModel card, IEnumerable<Creature> candidates)
    {
        var targets = candidates.ToArray();
        return targets.Length == 0
            ? null
            : card.Owner.RunState.Rng.CombatTargets.NextItem(targets);
    }
}
