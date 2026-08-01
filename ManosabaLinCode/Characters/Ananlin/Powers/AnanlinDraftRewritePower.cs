using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDraftRewritePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (CombatState is not { } combatState) return;

        var source = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1),
            CanRewriteSource,
            this)).FirstOrDefault();
        if (source is null) return;

        var rewritePools = BuildRewritePools(player, source).ToArray();
        if (rewritePools.Length == 0) return;

        var selectedRewritePool = player.RunState.Rng.CombatCardSelection.NextItem(rewritePools);
        var rewriteOptions = selectedRewritePool.Options;
        if (rewriteOptions.Length == 0) return;

        var rewritten = rewriteOptions.Length == 1
            ? rewriteOptions[0]
            : (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                rewriteOptions,
                player,
                new CardSelectorPrefs(new LocString("powers", $"{Id.Entry}.selectionScreenPrompt2"), 1, 1))).FirstOrDefault();
        if (rewritten is null) return;

        var canonical = rewritten.CanonicalInstance ?? ModelDb.GetById<CardModel>(rewritten.Id);
        if (canonical is null) return;

        Flash();
        var replacement = combatState.CreateCard(canonical, player);
        var transformResult = await CardCmd.Transform(source, replacement);
        var transformed = transformResult?.cardAdded ?? replacement;

        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        var poolWasRecorded = sketchbook is not null
            && await EnsurePoolRecorded(choiceContext, player, sketchbook, rewritten.Pool);

        if (poolWasRecorded)
            await AddPermanentCopyToDeck(player, transformed);
    }

    private IEnumerable<(CardPoolModel Pool, CardModel[] Options)> BuildRewritePools(
        Player player,
        CardModel source)
    {
        foreach (var pool in player.UnlockState.CharacterCardPools
                     .OrderBy(pool => pool.Id.Entry))
        {
            var options = BuildRewriteOptions(player, source, pool).ToArray();
            if (options.Length > 0)
                yield return (pool, options);
        }
    }

    private static IEnumerable<CardModel> BuildRewriteOptions(
        Player player,
        CardModel source,
        CardPoolModel pool)
    {
        var seenIds = new HashSet<ModelId>();

        foreach (var template in pool
                     .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                     .Where(template => IsRewriteTemplate(template, source))
                     .OrderBy(template => template.Id.Entry))
        {
            if (!seenIds.Add(template.Id)) continue;

            // Preview options must not enter CombatState; only the chosen card is created for the transform.
            var option = template.ToMutable();
            option.Owner = player;
            yield return option;
        }
    }

    private async Task<bool> EnsurePoolRecorded(
        PlayerChoiceContext choiceContext,
        Player player,
        AnansSketchbook sketchbook,
        CardPoolModel pool)
    {
        if (sketchbook.IsPoolRecorded(pool))
            return true;

        if (sketchbook.HasFullRecordedPools)
            await TryForgetOneRecordedPool(choiceContext, player, sketchbook);

        if (sketchbook.IsPoolRecorded(pool))
            return true;

        return sketchbook.TryRecordPoolWithFeedback(pool);
    }

    private async Task TryForgetOneRecordedPool(
        PlayerChoiceContext choiceContext,
        Player player,
        AnansSketchbook sketchbook)
    {
        var candidates = sketchbook
            .GetRecordedCardPools()
            .Select(pool => player.Deck.Cards.FirstOrDefault(card =>
                card.IsRemovable && card.Pool.Id == pool.Id))
            .OfType<CardModel>()
            .ToArray();

        if (candidates.Length == 0) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            player,
            new CardSelectorPrefs(new LocString("powers", $"{Id.Entry}.selectionScreenPrompt3"), 0, 1))).FirstOrDefault();
        if (selected is null) return;

        var poolToForget = selected.Pool;
        await CardPileCmd.RemoveFromDeck(selected);
        sketchbook.TryForgetRecordedPool(poolToForget);
    }

    private static async Task AddPermanentCopyToDeck(Player player, CardModel transformed)
    {
        var canonical = transformed.CanonicalInstance ?? ModelDb.GetById<CardModel>(transformed.Id);
        if (canonical is null)
            return;

        var permanentCard = player.RunState.CreateCard(canonical, player);

        for (var i = 0; i < transformed.CurrentUpgradeLevel; i++)
            CardCmd.Upgrade(permanentCard);

        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(permanentCard, PileType.Deck));
    }

    private static bool CanRewriteSource(CardModel card)
    {
        return card.Pile?.Type == PileType.Hand
            && card.IsTransformable
            && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    }

    private static bool IsRewriteTemplate(CardModel template, CardModel source)
    {
        return template.Id != source.Id
            && template.Type is CardType.Attack or CardType.Skill or CardType.Power
            && AnansSketchbook.CanSketchbookGenerate(template);
    }
}
