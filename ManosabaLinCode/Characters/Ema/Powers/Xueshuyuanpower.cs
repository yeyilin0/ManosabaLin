using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Xueshuyuanpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var bondPower = Owner.GetPower<BondPower>();
        if (bondPower == null) return;

        var temp = bondPower.Affinity;
        bondPower.Affinity = bondPower.Estrangement;
        bondPower.Estrangement = temp;

        Flash();
    }
}