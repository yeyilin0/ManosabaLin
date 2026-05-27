using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ManosabaLin.Characters.Common.GameActions;

public struct NetAamPowerChosenAction : INetAction
{
    public uint OwnerCombatId;
    public uint TargetCombatId;
    public int ChosenMoveIndex;


    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(OwnerCombatId);
        writer.WriteUInt(TargetCombatId);
        writer.WriteInt(ChosenMoveIndex);
    }

    public void Deserialize(PacketReader reader)
    {
        OwnerCombatId = reader.ReadUInt();
        TargetCombatId = reader.ReadUInt();
        ChosenMoveIndex = reader.ReadInt();
    }

    public GameAction ToGameAction(Player player)
    {
        return new AamPowerChosenAction(player, OwnerCombatId, TargetCombatId, ChosenMoveIndex);
    }
}
