using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSaveLoad()
    : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    [SavedProperty] public string StoredCardId { get; set; } = string.Empty;
    [SavedProperty] public int StoredUpgradeLevel { get; set; }

    private bool HasStoredCard => !string.IsNullOrWhiteSpace(StoredCardId);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

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

    public override void BetterAddExtraArgsToDescription(
        LocString description,
        PileType pileType,
        MinionLib.Utilities.BetterExtraArgs.DescriptionPreviewType previewType,
        Creature? target = null)
    {
        base.BetterAddExtraArgsToDescription(description, pileType, previewType, target);
        description.Add("ConsumedName", StoredCardTitleForDescription);
    }

    private string StoredCardTitleForDescription
    {
        get
        {
            if (GetStoredCanonicalCard() is not { } stored)
                return "未存入卡牌";

            var title = stored.TitleLocString.GetFormattedText();
            return StoredUpgradeLevel switch
            {
                <= 0 => title,
                1 => title + "+",
                _ => $"{title}+{StoredUpgradeLevel}"
            };
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
        if (cardPlay.Target is { } target)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(choiceContext);
        }

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
        await AnanlinCardHelpers.ResolveAsFreeCardEffect(choiceContext, copy, skipCardPileVisuals: false);
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
        EnergyCost.UpgradeBy(-1);
    }
}
