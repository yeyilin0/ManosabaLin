using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ManosabaLin.Characters.Common.GameActions;

public struct NetForceEndPlayerTurnAction : INetAction
{
    public int TurnNumber;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(TurnNumber);
    }

    public void Deserialize(PacketReader reader)
    {
        TurnNumber = reader.ReadInt();
    }

    public GameAction ToGameAction(Player player)
    {
        return new ForceEndPlayerTurnAction(player, TurnNumber);
    }
}
