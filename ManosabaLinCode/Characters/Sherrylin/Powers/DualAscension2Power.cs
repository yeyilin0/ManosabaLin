using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class DualAscension2Power : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    private int _cardsPlayed;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        _cardsPlayed++;

        if (_cardsPlayed < Amount) return;

        _cardsPlayed = 0;
        Flash();

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;

        var hand = PileType.Hand.GetPile(Owner.Player).Cards.ToList();
        if (hand.Count > 0)
        {
            var target = hand[rng.NextInt(hand.Count)];
            CardCmd.Upgrade(target);
        }

        var drawPile = PileType.Draw.GetPile(Owner.Player).Cards.ToList();
        if (drawPile.Count > 0)
        {
            var drawTarget = drawPile[rng.NextInt(drawPile.Count)];
            CardCmd.Upgrade(drawTarget);
        }
    }
}
