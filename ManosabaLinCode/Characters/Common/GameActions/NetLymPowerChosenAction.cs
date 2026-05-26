using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace ManosabaLin.Characters.Common.GameActions;

public struct NetLymPowerChosenAction : INetAction
{
    public uint OwnerCombatId;
    public uint TargetCombatId;


    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt(OwnerCombatId);
        writer.WriteUInt(TargetCombatId);
    }

    public void Deserialize(PacketReader reader)
    {
        OwnerCombatId = reader.ReadUInt();
        TargetCombatId = reader.ReadUInt();
    }

    public GameAction ToGameAction(Player player)
    {
        return new LymPowerChosenAction(player, OwnerCombatId, TargetCombatId);
    }
}
