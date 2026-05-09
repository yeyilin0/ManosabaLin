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

internal static class TransmigrationKeywordRegistration
{
    [RegisterOwnedCardKeyword("transmigration",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class TransmigrationKeyword;
}

public static class TransmigrationRules
{
    private const int MaxCopiesToPlay = 2;

    public static string TransmigrationKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "transmigration");

    public static bool HasTransmigration(CardModel? card)
    {
        return card != null && card.HasModKeyword(TransmigrationKeywordId);
    }

    private static List<CardModel> GetMatchingCardsFromDrawPile(CardModel source)
    {
        if (source?.Owner == null) return new List<CardModel>();

        var drawPile = PileType.Draw.GetPile(source.Owner);

        return drawPile.Cards
            .Where(c => c.Id == source.Id && c != source && HasTransmigration(c))
            .Take(MaxCopiesToPlay)
            .ToList();
    }

    public static async Task TriggerTransmigrationEffect(CardModel card, PlayerChoiceContext choiceContext,
        Creature? originalTarget)
    {
        if (!HasTransmigration(card)) return;

        var matchingCards = GetMatchingCardsFromDrawPile(card);

        if (matchingCards.Count == 0) return;

        var isFirst = true;
        foreach (var matchingCard in matchingCards)
        {
            matchingCard.SetToFreeThisTurn();

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