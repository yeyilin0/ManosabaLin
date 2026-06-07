using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class TheLionsManePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner.Player);
        var lionsMane = exhaustPile.Cards.FirstOrDefault(c => c.CanonicalInstance is Cards.TheLionsMane);
        if (lionsMane == null) return;

        Flash();

        await CardPileCmd.RemoveFromCombat(lionsMane);
        await CardPileCmd.Add(lionsMane, PileType.Hand);
        await CardCmd.AutoPlay(choiceContext, lionsMane, null);
    }
}
