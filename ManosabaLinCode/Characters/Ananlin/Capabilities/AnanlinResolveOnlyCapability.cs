using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Capabilities;

[RegisterModelCapability]
public sealed class AnanlinResolveOnlyCapability : ManosabaCardCapability, ICardPlayResultContributor
{
    public PileType? GetResultPileTypeForCardPlay(CardModel card) => PileType.None;
}
