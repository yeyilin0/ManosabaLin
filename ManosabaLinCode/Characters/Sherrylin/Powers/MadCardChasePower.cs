using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class MadCardChasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterAutoPostPlayPhaseEntered(
        PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var hand = PileType.Hand.GetPile(player).Cards.ToList();
        var playCount = hand.Count / 3;
        if (playCount <= 0) return;

        var candidates = hand.ToList();
        var rng = player.RunState.Rng.Shuffle;

        for (int i = 0; i < playCount && candidates.Count > 0; i++)
        {
            var card = rng.NextItem(candidates);
            if (card != null)
            {
                candidates.Remove(card);
                await CardCmd.AutoPlay(choiceContext, card, null);
            }
        }

        await PowerCmd.Remove(this);
    }
}
