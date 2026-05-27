using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ManosabaLin.Characters.Common.GameActions;

public sealed class AamPowerChosenAction(Player player, uint ownerCombatId, uint targetCombatId, int chosenMoveIndex) : GameAction
{
    public AamPowerChosenAction(Player player, Creature owner, Creature target, int chosenMoveIndex) : this(player,
        owner.CombatId ?? throw new ArgumentException("Owner must have a combat ID", nameof(owner)),
        target.CombatId ?? throw new ArgumentException("Target must have a combat ID", nameof(target)),
        chosenMoveIndex)
    {
    }

    protected override async Task ExecuteAction()
    {
        var owner = player.Creature.CombatState!.GetCreature(ownerCombatId);
        var target = player.Creature.CombatState.GetCreature(targetCombatId);
        if (owner is null || target is null) return;
        var power = owner.GetPower<AamPower>();
        if (power is null) return;
        await power.HandleGameAction(chosenMoveIndex, target);
    }

    public override INetAction ToNetAction()
    {
        return new NetAamPowerChosenAction
        {
            OwnerCombatId = ownerCombatId,
            TargetCombatId = targetCombatId,
            ChosenMoveIndex = chosenMoveIndex
        };
    }

    public override ulong OwnerId => player.NetId;

    public override GameActionType ActionType => GameActionType.CombatPlayPhaseOnly;
}
