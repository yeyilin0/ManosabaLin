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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Common.HiroKeywords;

internal static class Transmigration3KeywordRegistration
{
    [RegisterOwnedCardKeyword("transmigration3",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class Transmigration3Keyword;
}

public static class Transmigration3Rules
{
    private const int MaxCopiesToPlay = 2;

    public static string Transmigration3KeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "transmigration3");

    public static bool HasTransmigration3(CardModel? card)
    {
        return card != null && card.HasModKeyword(Transmigration3KeywordId);
    }

    private static List<CardModel> GetMatchingCardsFromAllPiles(CardModel source)
    {
        if (source?.Owner == null) return new List<CardModel>();

        var allCards = new List<CardModel>();

        var drawPile = PileType.Draw.GetPile(source.Owner);
        if (drawPile != null)
            allCards.AddRange(drawPile.Cards.Where(c => c != source && HasTransmigration3(c)));

        var handPile = PileType.Hand.GetPile(source.Owner);
        if (handPile != null)
            allCards.AddRange(handPile.Cards.Where(c => c != source && HasTransmigration3(c)));

        var discardPile = PileType.Discard.GetPile(source.Owner);
        if (discardPile != null)
            allCards.AddRange(discardPile.Cards.Where(c => c != source && HasTransmigration3(c)));

        var exhaustPile = PileType.Exhaust.GetPile(source.Owner);
        if (exhaustPile != null)
            allCards.AddRange(exhaustPile.Cards.Where(c => c != source && HasTransmigration3(c)));

        return allCards.Take(MaxCopiesToPlay).ToList();
    }

    public static async Task TriggerTransmigration3Effect(CardModel card, PlayerChoiceContext choiceContext,
        Creature? originalTarget)
    {
        if (!HasTransmigration3(card)) return;

        var matchingCards = GetMatchingCardsFromAllPiles(card);
        if (matchingCards.Count == 0) return;

        foreach (var matchingCard in matchingCards)
        {
            // 如果牌在手牌中，先移到弃牌堆再打出，避免递归
            if (matchingCard.Pile?.Type == PileType.Hand)
            {
                await CardPileCmd.Add(matchingCard, PileType.Discard);
            }

            matchingCard.SetToFreeThisTurn();

            await CardCmd.AutoPlay(
                choiceContext,
                matchingCard,
                originalTarget,
                skipCardPileVisuals: true
            );
        }
    }

    [RegisterSingleton]
    public sealed class Transmigration3Singleton : SingletonModel
    {
        public Transmigration3Singleton()
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
            // 自动打出的牌不再触发此效果，防止无限循环
            if (cardPlay.IsAutoPlay) return Task.CompletedTask;

            return TriggerTransmigration3Effect(cardPlay.Card, context, cardPlay.Target);
        }
    }
}