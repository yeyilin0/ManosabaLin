using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class NyxmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        Flash();

        var discardPile = PileType.Discard.GetPile(player);
        var cards = discardPile.Cards.ToList();

        if (cards.Count > 0)
        {
            var card = cards[new System.Random().Next(cards.Count)];
            await CardPileCmd.Add(card, PileType.Hand);
        }

        // 失去一层
        await PowerCmd.Decrement(this);
    }
}