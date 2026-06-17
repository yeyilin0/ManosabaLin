using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Sherrylin.Capabilities;

[RegisterModelCapability]
public class RemoveOnPlayCapability : ManosabaCardCapability, ICardPlayResultContributor
{
    public PileType? GetResultPileTypeForCardPlay(CardModel card) => PileType.None;
}
