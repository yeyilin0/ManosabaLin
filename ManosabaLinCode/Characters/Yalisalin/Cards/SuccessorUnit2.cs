using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     后继机2（0 费 技能・衍生 / Token）：
///     由「2887」生成，从所有有色（除无色之外）卡池中随机获得 1 张已升级的稀有牌加入手牌，然后从本场移除。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class SuccessorUnit2()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await ResolveSuccessor(choiceContext);

        await CardPileCmd.RemoveFromCombat(this);
    }

    private async Task ResolveSuccessor(PlayerChoiceContext choiceContext)
    {
        if (Owner is not { } owner)
            return;

        var pools = owner.UnlockState.CharacterCardPools
            .Where(p => p is not ColorlessCardPool)
            .ToArray();

        var options = CardCreationOptions
            .ForNonCombatWithUniformOdds(pools, c => c.Rarity == CardRarity.Rare)
            .GetPossibleCards(owner);

        var card = CardFactory
            .GetDistinctForCombat(owner, options, 1, owner.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();

        if (card is null)
            return;

        CardCmd.Upgrade(card, CardPreviewStyle.None);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
    }
}
