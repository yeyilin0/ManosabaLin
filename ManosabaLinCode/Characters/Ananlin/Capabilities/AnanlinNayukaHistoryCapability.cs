using System.Text.Json.Nodes;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinNayukaHistoryCapability : ManosabaCardCapability
{
    private const string SourceCardIdKey = "sourceCardId";

    public string SourceCardId { get; private set; } = string.Empty;

    public void Configure(CardModel source)
    {
        SourceCardId = source.Id.Entry;
        MarkDirty();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { } ownerCard) return;
        if (cardPlay.Card != ownerCard || !cardPlay.IsLastInSeries) return;
        if (string.IsNullOrWhiteSpace(SourceCardId)) return;

        var player = ownerCard.Owner;
        var toRemove = PileType.Exhaust.GetPile(player).Cards
            .Where(card => card.Id.Entry == SourceCardId)
            .ToArray();
        foreach (var card in toRemove)
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);

        if (ownerCard.Pile is { IsCombatPile: true })
            await CardPileCmd.RemoveFromCombat(ownerCard, skipVisuals: true);
    }

    protected override JsonNode? SaveAdditionalState()
    {
        return new JsonObject
        {
            [SourceCardIdKey] = SourceCardId
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        SourceCardId = state?[SourceCardIdKey]?.GetValue<string>() ?? string.Empty;
    }
}
