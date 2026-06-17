using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class Emaxielipower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var enemies = Owner.CombatState.Enemies.Where(e => e.IsAlive);
        foreach (var enemy in enemies)
        {
            await PowerCmd.Apply<EmaWitchFactorPower>(
                choiceContext, enemy, 10, Owner, null, false);
        }
    }
}