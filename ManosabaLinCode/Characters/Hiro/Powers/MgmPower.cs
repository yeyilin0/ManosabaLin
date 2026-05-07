using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class MgmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        var hand = PileType.Hand.GetPile(Owner.Player);

        var playableCards = hand.Cards
            .Where(c => !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();

        if (playableCards.Count == 0) return;

        for (int i = 0; i < Amount; i++)
        {
            var card = player.RunState.Rng.Shuffle.NextItem(playableCards);
            if (card != null)
                await CardCmd.AutoPlay(choiceContext, card, null);
        }

        Flash();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Decrement(this);
    }
}