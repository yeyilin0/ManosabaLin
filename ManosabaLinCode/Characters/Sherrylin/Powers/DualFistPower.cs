// DualFistPower.cs
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class DualFistPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var rng = player.RunState.Rng.CombatCardSelection;
        if (rng.NextInt(2) == 0)
            await PowerCmd.Apply<LeftFistPower>(choiceContext, Owner, 1, Owner, null, false);
        else
            await PowerCmd.Apply<RightFistPower>(choiceContext, Owner, 1, Owner, null, false);
    }
}