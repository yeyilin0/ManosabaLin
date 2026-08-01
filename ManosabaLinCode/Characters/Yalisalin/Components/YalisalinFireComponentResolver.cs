using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Hooks;
using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Components;

public static class YalisalinFireComponentResolver
{
    private static readonly HashSet<CardModel> ResolvingCards = [];
    private static readonly HashSet<CardModel> SuppressedCards = [];

    public static async Task<YalisalinFireComponentContext> Resolve(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        YalisalinFireComponentCapability component)
    {
        return await Resolve(
            choiceContext,
            cardPlay,
            component,
            sourceAlreadyPlaying: true,
            countsAsManualUse: !cardPlay.IsAutoPlay);
    }

    public static async Task<YalisalinFireComponentContext?> ResolveFromCard(
        PlayerChoiceContext choiceContext,
        CardModel source,
        Creature? target = null,
        bool countsAsManualUse = true,
        int temporarySourceCostReduction = 0)
    {
        if (!source.TryGetCapability<YalisalinFireComponentCapability>(out var component))
            return null;

        var cardPlay = new CardPlay
        {
            Card = source,
            Player = source.Owner,
            Target = target,
            ResultPile = PileType.Discard,
            Resources = new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = source.EnergyCost.GetAmountToSpend(),
                StarsSpent = 0,
                StarValue = Math.Max(0, source.GetStarCostWithModifiers())
            },
            IsAutoPlay = false,
            PlayIndex = 0,
            PlayCount = 1
        };

        var context = await Resolve(
            choiceContext,
            cardPlay,
            component,
            sourceAlreadyPlaying: false,
            countsAsManualUse: countsAsManualUse,
            initialSourceCostReduction: temporarySourceCostReduction);

        return context;
    }

    internal static bool IsSuppressed(CardModel card)
    {
        return SuppressedCards.Contains(card);
    }

    private static async Task<YalisalinFireComponentContext> Resolve(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        YalisalinFireComponentCapability component,
        bool sourceAlreadyPlaying,
        bool countsAsManualUse,
        int initialSourceCostReduction = 0)
    {
        var source = component.Owner ?? cardPlay.Card;
        var owner = source.Owner;
        var context = new YalisalinFireComponentContext(
            owner,
            cardPlay,
            component,
            sourceAlreadyPlaying,
            countsAsManualUse);
        context.ShouldAutoPlaySourceChoice = !sourceAlreadyPlaying;
        if (initialSourceCostReduction != 0)
            context.AddTemporaryCostOffset(source, -Math.Abs(initialSourceCostReduction));

        if (cardPlay.IsAutoPlay || IsSuppressed(source) || !ResolvingCards.Add(source))
            return context;

        try
        {
            BuildDefaultConnectionPool(context);
            var modifiers = GetModifiers(context).ToArray();

            foreach (var modifier in modifiers)
                modifier.ModifyFireComponentConnectionPool(context);

            if (!context.ShouldResolve || context.ConnectionPool.Count == 0)
                return context;

            context.LinkedCard = owner.RunState.Rng.CombatCardSelection.NextItem(context.ConnectionPool);
            if (context.LinkedCard == null)
                return context;

            context.AddChoiceOption(source);
            if (context.LinkedCard != source)
                context.AddChoiceOption(context.LinkedCard);

            foreach (var modifier in modifiers)
                modifier.ModifyFireComponentChoiceOptions(context);

            foreach (var modifier in modifiers)
                modifier.ModifyFireComponentRightClickQueue(context);

            context.ChoiceOptions.RemoveAll(card => card == null || card.HasBeenRemovedFromState);
            if (context.ChoiceOptions.Count == 0)
                return context;

            context.ChosenCard = await ChooseCard(choiceContext, context);
            context.ChosenCard ??= source;
            ForcePlayableChoiceIfNeeded(context);
            context.ChoiceCompleted = true;

            foreach (var modifier in modifiers)
                await modifier.AfterFireComponentChoiceCompleted(choiceContext, context);

            await ResolveAppliedRightClicks(choiceContext, context);

            var burnQueue = DetermineBurnQueue(context).ToArray();
            foreach (var burned in burnQueue)
            {
                context.BurnedCard = burned;

                foreach (var modifier in modifiers)
                    await modifier.BeforeFireComponentBurned(choiceContext, context);

                if (context.BurnedCard != null)
                    await TryAutoPlayBurnedCard(choiceContext, context);

                await Burn(choiceContext, context);

                foreach (var modifier in modifiers)
                    await modifier.AfterFireComponentBurned(choiceContext, context);
            }

            if (context.ChosenCard != source && context.ShouldAutoPlayLinkedChoice)
            {
                context.ShouldSkipSourceCardCore = true;
                await PlayChosenCardIfPossible(choiceContext, context, context.ChosenCard);
            }
            else if (!context.SourceAlreadyPlaying && context.ShouldAutoPlaySourceChoice)
            {
                await PlayChosenCardIfPossible(choiceContext, context, context.ChosenCard);
            }

            foreach (var burned in context.ExclusiveBurnCards.Where(card => card == context.ChosenCard).ToArray())
            {
                context.BurnedCard = burned;

                foreach (var modifier in modifiers)
                    await modifier.BeforeFireComponentBurned(choiceContext, context);

                await Burn(choiceContext, context);

                foreach (var modifier in modifiers)
                    await modifier.AfterFireComponentBurned(choiceContext, context);
            }

            foreach (var modifier in modifiers)
                await modifier.AfterFireComponentResolved(choiceContext, context);

            return context;
        }
        finally
        {
            ResolvingCards.Remove(source);
        }
    }

    private static void BuildDefaultConnectionPool(YalisalinFireComponentContext context)
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(context.Owner).Cards)
                context.AddConnectionCandidate(card);
        }
    }

    private static async Task<CardModel?> ChooseCard(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (!context.ShouldPrompt || context.ChoiceOptions.Count == 1)
            return context.ChoiceOptions[0];

        using var scope = YalisalinFireComponentSelectionRegistry.Begin(context);
        return (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            context.ChoiceOptions,
            context.Owner,
            new CardSelectorPrefs(
                BuildSelectionPrompt(context),
                1,
                1))).FirstOrDefault();
    }

    private static LocString BuildSelectionPrompt(YalisalinFireComponentContext context)
    {
        var loc = new LocString("cards", "ManosabaLin.YalisalinFireComponent.dynamicSelectionScreenPrompt");
        loc.Add("Prompt", context.SelectionPromptText);
        return loc;
    }

    private static IEnumerable<CardModel> DetermineBurnQueue(YalisalinFireComponentContext context)
    {
        if (context.ExclusiveBurnCards.Count > 0)
        {
            foreach (var card in context.ExclusiveBurnCards.Where(card => card != context.ChosenCard))
                yield return card;

            if (context.BurnOnlyExclusiveCards)
                yield break;
        }

        var burned = context.ChoiceOptions.FirstOrDefault(card => card != context.ChosenCard);
        if (burned != null && !context.ExclusiveBurnCards.Contains(burned))
            yield return burned;
    }

    private static async Task ResolveAppliedRightClicks(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        foreach (var applied in context.AppliedRightClicks)
        {
            if (applied.Kind != YalisalinFireRightClickKind.FifthSelfProof)
                continue;

            if (applied.Source is not ManosabaLin.Characters.Yalisalin.Cards.Fifthselfproof fifth
                || fifth.FireUseCount <= 0)
                continue;

            fifth.FireUseCount--;

            if (applied.Card.Type is CardType.Attack or CardType.Skill)
            {
                var capability = applied.Card.GetOrCreateCapability<YalisalinFifthSelfProofStrengthCapability>();
                capability.Add(applied.Card.Type);
                continue;
            }

            if (applied.Card.Type == CardType.Power && applied.Card == context.ChosenCard)
                await GenerateDiscountedFirePower(choiceContext, context.Owner, applied.Card.IsUpgraded);
        }
    }

    private static async Task GenerateDiscountedFirePower(
        PlayerChoiceContext choiceContext,
        Player owner,
        bool upgraded)
    {
        var generated = YalisalinFireComponentRules.RandomYalisalinCard(owner, CardType.Power);
        if (generated == null)
            return;

        if (upgraded)
            CardCmd.Upgrade(generated);

        generated.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
        YalisalinFireComponentRules.TryAddFireComponent(generated);
        await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner);
    }

    private static async Task TryAutoPlayBurnedCard(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.CustomData.GetValueOrDefault("AutoPlayBurnedCard") is not true)
            return;

        if (context.BurnedCard is not { } burned
            || !CanPlayFromFireComponent(context, burned, ResolveTarget(burned, context.Target)))
            return;

        await PlayCardWithSuppressedFireComponent(choiceContext, context, burned);
    }

    private static async Task PlayChosenCardIfPossible(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context,
        CardModel? card)
    {
        if (card == null)
            return;

        var target = ResolveTarget(card, context.Target);
        if (!CanPlayFromFireComponent(context, card, target))
            return;

        await PlayCardWithSuppressedFireComponent(choiceContext, context, card);
    }

    private static void ForcePlayableChoiceIfNeeded(YalisalinFireComponentContext context)
    {
        if (context.ChosenCard is not { } chosen)
            return;

        if (CanResolveChosenCard(context, chosen, includePendingChoiceEffects: true))
            return;

        var fallback = context.ChoiceOptions.FirstOrDefault(card =>
            card != chosen && CanResolveChosenCard(context, card, includePendingChoiceEffects: true));
        if (fallback != null)
            context.ChosenCard = fallback;
    }

    private static bool CanResolveChosenCard(
        YalisalinFireComponentContext context,
        CardModel card,
        bool includePendingChoiceEffects = false)
    {
        if (card == context.SourceCard)
        {
            if (context.SourceAlreadyPlaying || !context.ShouldAutoPlaySourceChoice)
                return true;
        }
        else if (!context.ShouldAutoPlayLinkedChoice)
        {
            return true;
        }

        return CanPlayFromFireComponent(
            context,
            card,
            ResolveTarget(card, context.Target),
            includePendingChoiceEffects);
    }

    private static bool CanPlayFromFireComponent(
        YalisalinFireComponentContext context,
        CardModel card,
        Creature? target,
        bool includePendingChoiceEffects = false)
    {
        if (card.HasBeenRemovedFromState)
            return false;

        if (card.Keywords.Contains(CardKeyword.Unplayable))
            return false;

        var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        if (combatState == null || card.Owner.PlayerCombatState == null)
            return false;

        if (!card.IsValidTarget(target))
            return false;

        if (!card.CanPlay(out var reason, out _))
        {
            var nonResourceReasons = reason
                                     & ~UnplayableReason.EnergyCostTooHigh
                                     & ~UnplayableReason.StarCostTooHigh;
            if (nonResourceReasons != UnplayableReason.None)
                return false;
        }

        return HasEnoughResourcesForFireComponent(
            context,
            card,
            combatState,
            includePendingChoiceEffects);
    }

    private static bool HasEnoughResourcesForFireComponent(
        YalisalinFireComponentContext context,
        CardModel card,
        ICombatState combatState,
        bool includePendingChoiceEffects)
    {
        var playerCombatState = card.Owner.PlayerCombatState;
        if (playerCombatState == null)
            return false;

        var energyToSpend = context.GetEffectiveCost(
            card,
            includePendingChoiceEffects ? GetPendingChoiceCostOffset(context, card) : 0);
        var starsToSpend = Math.Max(0, card.GetStarCostWithModifiers());

        if (energyToSpend > playerCombatState.Energy
            && Hook.ShouldPayExcessEnergyCostWithStars(combatState, card.Owner))
        {
            starsToSpend += (energyToSpend - playerCombatState.Energy) * 2;
            energyToSpend = playerCombatState.Energy;
        }

        return energyToSpend <= playerCombatState.Energy
               && starsToSpend <= playerCombatState.Stars;
    }

    private static int GetPendingChoiceCostOffset(YalisalinFireComponentContext context, CardModel card)
    {
        if (card == context.SourceCard)
            return 0;

        return YalisalinFireColorSystem.TryGetHairpin(context.Owner, out var hairpin)
               && hairpin.SeparatedEndsEnabled
            ? -1
            : 0;
    }

    private static async Task PlayCardWithSuppressedFireComponent(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context,
        CardModel card)
    {
        var target = ResolveTarget(card, context.Target);
        var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        if (combatState == null)
            return;

        if (!CanPlayFromFireComponent(context, card, target))
            return;

        context.ApplyTemporaryCostOffset(card);
        if (!CanPlayFromFireComponent(context, card, target))
            return;

        await MoveCardToPlayPileForFireComponent(card);
        if (card.CombatState == null)
            return;

        var (energySpent, starsSpent) = await card.SpendResources();
        var resources = new ResourceInfo
        {
            EnergySpent = energySpent,
            EnergyValue = energySpent,
            StarsSpent = starsSpent,
            StarValue = starsSpent
        };

        SuppressedCards.Add(card);
        try
        {
            await card.OnPlayWrapper(choiceContext, target, isAutoPlay: true, resources, skipCardPileVisuals: true);
        }
        finally
        {
            SuppressedCards.Remove(card);
        }
    }

    private static async Task MoveCardToPlayPileForFireComponent(CardModel card)
    {
        if (card.Pile?.Type == PileType.Play)
            return;

        await CardPileCmd.Add(card, PileType.Play, skipVisuals: true);
    }

    private static async Task Burn(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.BurnedCard is not { } burned || burned.HasBeenRemovedFromState)
            return;

        switch (context.BurnMode)
        {
            case YalisalinFireComponentBurnMode.Exhaust:
                await CardCmd.Exhaust(choiceContext, burned, skipVisuals: context.SkipBurnVisuals);
                context.MarkBurned(burned);
                break;
            case YalisalinFireComponentBurnMode.RemoveFromCombat:
                await CardPileCmd.RemoveFromCombat(burned, context.SkipBurnVisuals);
                context.MarkBurned(burned);
                break;
            case YalisalinFireComponentBurnMode.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(context.BurnMode), context.BurnMode, null);
        }
    }

    private static Creature? ResolveTarget(CardModel card, Creature? preferredTarget)
    {
        var combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        if (combatState == null)
            return null;

        return card.TargetType switch
        {
            TargetType.AnyEnemy when preferredTarget is { IsAlive: true }
                                     && preferredTarget.Side != card.Owner.Creature.Side => preferredTarget,
            TargetType.AnyAlly when preferredTarget is { IsAlive: true }
                                    && preferredTarget.Side == card.Owner.Creature.Side => preferredTarget,
            TargetType.AnyEnemy => card.Owner.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies),
            TargetType.AnyAlly => card.Owner.RunState.Rng.CombatTargets.NextItem(
                combatState.Allies.Where(creature =>
                    creature.IsAlive
                    && creature.IsPlayer
                    && creature != card.Owner.Creature)),
            _ => null
        };
    }

    private static IEnumerable<IYalisalinFireComponentModifier> GetModifiers(YalisalinFireComponentContext context)
    {
        HashSet<object> seen = [];

        foreach (var modifier in EnumerateModifierObjects(context))
        {
            if (!seen.Add(modifier)) continue;
            yield return modifier;
        }
    }

    private static IEnumerable<IYalisalinFireComponentModifier> EnumerateModifierObjects(
        YalisalinFireComponentContext context)
    {
        foreach (var relic in context.Owner.Relics.OfType<IYalisalinFireComponentModifier>())
            yield return relic;

        foreach (var power in context.Owner.Creature.Powers.OfType<IYalisalinFireComponentModifier>())
            yield return power;

        if (context.Owner.Creature.CombatState is { } combatState)
        {
            foreach (var creature in combatState.Creatures)
            {
                foreach (var power in creature.Powers.OfType<IYalisalinFireComponentModifier>())
                    yield return power;
            }
        }

        foreach (var card in YalisalinFireComponentRules.AllCombatCards(context.Owner))
        foreach (var modifier in YalisalinFireComponentRules.CardModifiers(card))
            yield return modifier;
    }
}
