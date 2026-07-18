using System.Text.Json.Nodes;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinCocoMultiverseCapability : ManosabaCardCapability
{
    private const string SelectedCardIdsKey = "selectedCardIds";
    private const string CardsLocArgument = "Cards";

    private List<string> SelectedCardIds { get; set; } = [];

    public void RerollFromRecordedPools(AnansSketchbook? sketchbook)
    {
        if (sketchbook is null)
        {
            SelectedCardIds = [];
            MarkDirty();
            return;
        }

        var rng = sketchbook.Owner.RunState.Rng.CombatCardGeneration;
        SelectedCardIds = sketchbook.GetRecordedCardPools()
            .Select(pool => sketchbook.GetRecordableCardsFromPool(pool).ToArray())
            .Where(static cards => cards.Length > 0)
            .Select(cards => rng.NextItem(cards))
            .OfType<CardModel>()
            .Select(static card => card.Id.Entry)
            .ToList();
        MarkDirty();
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Owner != player) return Task.CompletedTask;
        if (Owner.Pile?.Type != PileType.Hand) return Task.CompletedTask;

        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        RerollFromRecordedPools(sketchbook);
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != Owner || !cardPlay.IsLastInSeries) return;
        if (Owner is null) return;

        var cardsToPlay = CreateSelectedCards(Owner).ToArray();
        foreach (var card in cardsToPlay)
            await AnanlinCardHelpers.ResolveAsFreeCardEffect(choiceContext, card);
    }

    public override IEnumerable<IHoverTip> GetHoverTips(CardModel card)
    {
        foreach (var tip in base.GetHoverTips(card))
            yield return tip;

        foreach (var selectedCard in GetSelectedCanonicalCards())
            yield return new CardHoverTip(selectedCard);
    }

    protected override void AddExtraLocArguments(LocString loc)
    {
        loc.Add(CardsLocArgument, SelectedCardsText);
    }

    protected override JsonNode? SaveAdditionalState()
    {
        var selectedCardIds = new JsonArray();
        foreach (var cardId in SelectedCardIds)
            selectedCardIds.Add(cardId);

        return new JsonObject
        {
            [SelectedCardIdsKey] = selectedCardIds
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        SelectedCardIds = state?[SelectedCardIdsKey] is JsonArray array
            ? array.Select(static node => node?.GetValue<string>())
                .OfType<string>()
                .ToList()
            : [];
    }

    private IEnumerable<CardModel> CreateSelectedCards(CardModel ownerCard)
    {
        if (ownerCard.CombatState is not { } combatState) yield break;

        foreach (var canonical in GetSelectedCanonicalCards())
            yield return combatState.CreateCard(canonical, ownerCard.Owner);
    }

    private IEnumerable<CardModel> GetSelectedCanonicalCards()
    {
        foreach (var cardId in SelectedCardIds)
        {
            var canonical = ModelDb.GetByIdOrNull<CardModel>(new ModelId("CARD", cardId));
            if (canonical is not null)
                yield return canonical;
        }
    }

    private string SelectedCardsText
    {
        get
        {
            var titles = GetSelectedCanonicalCards()
                .Select(static card => card.Title)
                .ToArray();
            return titles.Length == 0
                ? new LocString("cards", $"{LocKeyPrefix}.empty").GetFormattedText()
                : string.Join(", ", titles);
        }
    }

}
