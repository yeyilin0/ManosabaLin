using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Common.HiroKeywords;

/// <summary>
///     轮回关键词注册
/// </summary>
internal static class TransmigrationKeywordRegistration
{
    [RegisterOwnedCardKeyword("transmigration",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class TransmigrationKeyword;
}

/// <summary>
///     轮回关键词规则（包含完整效果）
/// </summary>
public static class TransmigrationRules
{
    private const int MaxCopiesToPlay = 2;

    public static string TransmigrationKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "transmigration");

    /// <summary>
    ///     检查卡牌是否有轮回关键词
    /// </summary>
    public static bool HasTransmigration(CardModel? card)
    {
        return card != null && card.HasModKeyword(TransmigrationKeywordId);
    }

    /// <summary>
    ///     从抽牌堆获取同名卡牌（最多2张，排除自己；且这些卡也必须带轮回关键词）
    /// </summary>
    private static List<CardModel> GetMatchingCardsFromDrawPile(CardModel source)
    {
        if (source?.Owner == null) return new List<CardModel>();

        var drawPile = PileType.Draw.GetPile(source.Owner);

        return drawPile.Cards
            .Where(c => c.Id == source.Id && c != source && HasTransmigration(c))
            .Take(MaxCopiesToPlay)
            .ToList();
    }

    /// <summary>
    ///     执行轮回效果（自动打出抽牌堆中的同名卡牌）
    /// </summary>
    public static async Task TriggerTransmigrationEffect(CardModel card, PlayerChoiceContext choiceContext,
        Creature? originalTarget)
    {
        // 检查是否有轮回关键词
        if (!HasTransmigration(card)) return;

        // 从抽牌堆获取同名卡牌
        var matchingCards = GetMatchingCardsFromDrawPile(card);

        if (matchingCards.Count == 0) return;

        var isFirst = true;
        foreach (var matchingCard in matchingCards)
        {
            // 本回合免费
            matchingCard.SetToFreeThisTurn();

            await CardPileCmd.Add(matchingCard, PileType.Play);

            // 自动打出
            await CardCmd.AutoPlay(
                choiceContext,
                matchingCard,
                originalTarget,
                skipCardPileVisuals: !isFirst
            );

            isFirst = false;
            await Cmd.Wait(0.1f);
        }
    }
}

[RegisterSingleton]
public sealed class TransmigrationSingleton : SingletonModel
{
    public TransmigrationSingleton()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay) return Task.CompletedTask;
        return TransmigrationRules.TriggerTransmigrationEffect(cardPlay.Card, context, cardPlay.Target);
    }
}