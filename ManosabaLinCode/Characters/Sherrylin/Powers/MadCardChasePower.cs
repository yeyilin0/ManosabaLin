using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class MadCardChasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Any(p => p == Owner)) return;

        var hand = PileType.Hand.GetPile(Owner.Player).Cards.ToList();
        var playCount = hand.Count / 3;
        if (playCount <= 0) return;

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var candidates = hand.ToList();

        for (int i = 0; i < playCount && candidates.Count > 0; i++)
        {
            var idx = rng.NextInt(candidates.Count);
            var card = candidates[idx];
            candidates.RemoveAt(idx);
            await CardCmd.AutoPlay(choiceContext, card, null);
        }

        await PowerCmd.Remove(this);
    }
}
