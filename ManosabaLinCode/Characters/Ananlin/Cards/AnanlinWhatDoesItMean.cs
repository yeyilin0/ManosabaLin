using ManosabaLin.Characters.Ananlin.Capabilities;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinWhatDoesItMean()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private enum RandomEffect
    {
        Free,
        DoubleDamage,
        Exhaust,
        Replace
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var target = PickRandomHandCard();
        if (target is null) return;

        var effect = Owner.RunState.Rng.CombatCardGeneration.NextItem(Enum.GetValues<RandomEffect>());
        if (IsUpgraded && !await ShouldAccept(choiceContext, target, effect))
            return;

        await ApplyEffect(choiceContext, target, effect);
    }

    private CardModel? PickRandomHandCard()
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card != this && AnanlinCardHelpers.IsPlayableCombatCard(card))
            .ToArray();

        return candidates.Length == 0
            ? null
            : Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
    }

    private async Task<bool> ShouldAccept(PlayerChoiceContext choiceContext, CardModel target, RandomEffect effect)
    {
        var accept = CombatState.CreateCard<AnanlinWhatDoesItMeanAcceptOption>(Owner);
        var reject = CombatState.CreateCard<AnanlinWhatDoesItMeanRejectOption>(Owner);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            [accept, reject],
            Owner,
            new CardSelectorPrefs(new LocString("cards", $"{Id.Entry}.selectionScreenPrompt"), 1, 1))).FirstOrDefault();

        return selected is AnanlinWhatDoesItMeanAcceptOption;
    }

    private async Task ApplyEffect(PlayerChoiceContext choiceContext, CardModel target, RandomEffect effect)
    {
        switch (effect)
        {
            case RandomEffect.Free:
                target.EnergyCost.SetThisTurnOrUntilPlayed(0, reduceOnly: true);
                break;
            case RandomEffect.DoubleDamage:
                target.GetOrCreateCapability<AnanlinDoubleDamageOnceCapability>();
                break;
            case RandomEffect.Exhaust:
                CardCmd.ApplyKeyword(target, CardKeyword.Exhaust);
                break;
            case RandomEffect.Replace:
                await ReplaceWithSameRarity(choiceContext, target);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
        }
    }

    private async Task ReplaceWithSameRarity(PlayerChoiceContext choiceContext, CardModel target)
    {
        if (!target.IsTransformable) return;

        var candidates = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == target.Rarity
                && card.Id != target.Id
                && card.CanBeGeneratedInCombat)
            .ToArray();

        if (candidates.Length == 0) return;

        var replacement = CardFactory.GetForCombat(
                Owner,
                candidates,
                1,
                Owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (replacement is null) return;

        AnanlinCardHelpers.CopyUpgradeLevel(target, replacement);
        await CardCmd.Transform(target, replacement, CardPreviewStyle.None);
    }
}
