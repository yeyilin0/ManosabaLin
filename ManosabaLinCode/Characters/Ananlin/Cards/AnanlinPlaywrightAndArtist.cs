using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinPlaywrightAndArtist() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const int OptionCount = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var sourceOptions = GetRewriteSources().ToList();
        if (sourceOptions.Count == 0) return;

        var selectedSource = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            sourceOptions,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
        if (selectedSource is null) return;

        var selectedFromRecordedPool = this.Sketchbook()?.IsFromRecordedPool(selectedSource) == true;
        var rewriteOptions = BuildRewriteOptions(selectedSource, selectedFromRecordedPool).ToList();
        if (rewriteOptions.Count == 0) return;

        var rewritten = rewriteOptions.Count == 1
            ? rewriteOptions[0]
            : (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                rewriteOptions,
                Owner,
                new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt2"), 1, 1))).FirstOrDefault();
        if (rewritten is null) return;

        await CardPileCmd.RemoveFromCombat(selectedSource);
        await CardPileCmd.AddGeneratedCardToCombat(rewritten, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }

    private IEnumerable<CardModel> GetRewriteSources()
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .SelectMany(pile => pile.GetPile(Owner).Cards)
            .Where(card => card != this && CanRewriteSource(card));
    }

    private IReadOnlyList<CardModel> BuildRewriteOptions(CardModel source, bool selectedFromRecordedPool)
    {
        if (CombatState is not { } combatState) return [];

        var sketchbook = this.Sketchbook();
        var recordedPools = sketchbook?.GetRecordedCardPools() ?? [];
        var options = recordedPools.Count > 0
            ? BuildRewriteOptionsFromPools(recordedPools, source, selectedFromRecordedPool, combatState).ToList()
            : [];

        if (options.Count == 0)
            options = BuildRewriteOptionsFromPools(Owner.UnlockState.CharacterCardPools, source, selectedFromRecordedPool, combatState).ToList();

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        return options
            .OrderBy(_ => rng.NextFloat())
            .Take(OptionCount)
            .ToArray();
    }

    private IEnumerable<CardModel> BuildRewriteOptionsFromPools(
        IEnumerable<CardPoolModel> pools,
        CardModel source,
        bool selectedFromRecordedPool,
        ICombatState combatState)
    {
        var seenIds = new HashSet<ModelId>();

        foreach (var pool in pools)
        {
            var templates = pool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(template => IsRewriteTemplate(template, source));

            foreach (var template in templates)
            {
                if (!seenIds.Add(template.Id)) continue;

                var rewritten = combatState.CreateCard(template, Owner);
                CopyUpgradeLevel(source, rewritten);
                ApplyRewriteModifiers(rewritten, selectedFromRecordedPool);

                if (IsCurrentlyPlayable(rewritten, combatState))
                    yield return rewritten;
            }
        }
    }

    private static bool CanRewriteSource(CardModel card)
    {
        return card.Type is CardType.Attack or CardType.Skill or CardType.Power
            && card.Rarity is not CardRarity.Status
            and not CardRarity.Curse
            and not CardRarity.Quest
            and not CardRarity.Event
            and not CardRarity.Token;
    }

    private static bool IsRewriteTemplate(CardModel template, CardModel source)
    {
        return template.Id != source.Id
            && template.Type == source.Type
            && !template.EnergyCost.CostsX
            && CanRewriteSource(template)
            && AnansSketchbook.CanSketchbookGenerate(template);
    }

    private static void CopyUpgradeLevel(CardModel source, CardModel target)
    {
        for (var i = 0; i < source.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(target);
    }

    private static void ApplyRewriteModifiers(CardModel card, bool selectedFromRecordedPool)
    {
        if (selectedFromRecordedPool)
            card.SetToFreeThisTurn();
        else
            card.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);

        card.AddKeyword(CardKeyword.Retain);
        card.AddKeyword(CardKeyword.Ethereal);
    }

    private static bool IsCurrentlyPlayable(CardModel card, ICombatState combatState)
    {
        if (!HasValidTarget(card, combatState)) return false;

        return card.CanPlay();
    }

    private static bool HasValidTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.Enemies.Any(card.IsValidTarget),
            TargetType.AnyAlly => combatState.PlayerCreatures.Any(card.IsValidTarget),
            _ => card.IsValidTarget(null)
        };
    }
}
