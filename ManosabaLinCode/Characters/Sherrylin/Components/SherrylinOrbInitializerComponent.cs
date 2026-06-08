using MegaCrit.Sts2.Core.Nodes.Rooms;
using MinionLib.Component.Utils;

namespace ManosabaLin.Characters.Sherrylin.Components;

/// <summary>
/// 战斗开始时为雪莉琳扩展2个充能球槽位。
/// </summary>
public sealed partial class SherrylinOrbInitializerComponent : TimingCardComponent
{
    private const int TargetSlots = 2;

    public SherrylinOrbInitializerComponent()
        : base(Timing.BeforeCombatStart, Timing.AfterCardEnteredCombat) { }

    protected override async Task OnTimingPostfix(OnTimingContext context)
    {
        if (Card?.Owner?.Creature == null) return;
        var queue = Card.Owner.PlayerCombatState?.OrbQueue;
        if (queue == null) return;

        int addAmount = TargetSlots - queue.Capacity;
        if (addAmount <= 0) return;

        queue.AddCapacity(addAmount);
        await Task.Yield();

        NCombatRoom.Instance?
            .GetCreatureNode(Card.Owner.Creature)?
            .OrbManager?
            .AddSlotAnim(addAmount);
    }
}
