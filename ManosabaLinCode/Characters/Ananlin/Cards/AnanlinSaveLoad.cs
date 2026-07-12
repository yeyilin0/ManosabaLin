using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSaveLoad()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    [SavedProperty] public string StoredCardId { get; set; } = string.Empty;
    [SavedProperty] public int StoredUpgradeLevel { get; set; }

    private bool HasStoredCard => !string.IsNullOrWhiteSpace(StoredCardId);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (GetStoredCanonicalCard() is { } stored)
                yield return new CardHoverTip(stored);

            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
            if (Keywords.Contains(CardKeyword.Retain))
                yield return HoverTipFactory.FromKeyword(CardKeyword.Retain);
        }
    }

    protected override bool IsPlayableC =>
        base.IsPlayableC
        && (GetStoredCanonicalCard() is not null || HasRecordableExhaustCardInHand());

    protected override (PileType, CardPilePosition) GetResultPileTypeAndPositionForCardPlayC()
    {
        return HasStoredCard
            ? base.GetResultPileTypeAndPositionForCardPlayC()
            : (PileType.Hand, CardPilePosition.Bottom);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (!HasStoredCard)
        {
            await StoreExhaustCard(choiceContext);
            return;
        }

        await PlayStoredCard(choiceContext);
    }

    private async Task StoreExhaustCard(PlayerChoiceContext choiceContext)
    {
        var selected = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, 1),
                IsRecordableExhaustCard,
                this))
            .FirstOrDefault();

        if (selected is null) return;

        StoredCardId = selected.Id.Entry;
        StoredUpgradeLevel = selected.CurrentUpgradeLevel;
        await CardCmd.Exhaust(choiceContext, selected);
    }

    private async Task PlayStoredCard(PlayerChoiceContext choiceContext)
    {
        if (GetStoredCanonicalCard() is not { } canonical) return;
        if (Owner.Creature.CombatState is not { } combatState) return;

        var copy = combatState.CreateCard(canonical, Owner);
        ApplyUpgradeLevels(copy, StoredUpgradeLevel);
        copy.AddKeyword(CardKeyword.Exhaust);

        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
        await CardCmd.AutoPlay(
            choiceContext,
            copy,
            PickTarget(copy),
            skipXCapture: true);
    }

    private bool HasRecordableExhaustCardInHand()
    {
        return PileType.Hand.GetPile(Owner).Cards.Any(IsRecordableExhaustCard);
    }

    private bool IsRecordableExhaustCard(CardModel card)
    {
        return card != this
            && !card.Keywords.Contains(CardKeyword.Unplayable)
            && (card.Keywords.Contains(CardKeyword.Exhaust) || card.ExhaustOnNextPlay);
    }

    private CardModel? GetStoredCanonicalCard()
    {
        if (!HasStoredCard) return null;
        return ModelDb.GetByIdOrNull<CardModel>(new ModelId("CARD", StoredCardId));
    }

    private Creature? PickTarget(CardModel card)
    {
        if (Owner.Creature.CombatState is not { } combatState) return null;

        if (card.TargetType is TargetType.AnyEnemy or TargetType.RandomEnemy)
        {
            var enemies = combatState.HittableEnemies.ToArray();
            return enemies.Length == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        }

        if (card.TargetType == TargetType.AnyAlly)
        {
            var allies = combatState.Creatures
                .Where(c => c.Side == Owner.Creature.Side && c.IsAlive && c != Owner.Creature)
                .ToArray();
            return allies.Length == 0 ? null : Owner.RunState.Rng.CombatTargets.NextItem(allies);
        }

        if (card.TargetType == TargetType.AnyPlayer)
            return Owner.Creature;

        return null;
    }

    private static void ApplyUpgradeLevels(CardModel card, int upgradeLevel)
    {
        for (var i = 0; i < upgradeLevel && card.IsUpgradable; i++)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Retain);
    }
}
