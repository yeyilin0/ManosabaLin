using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Helpers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class Geinizhengyipower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private readonly Dictionary<Creature, bool> _energyGained = new();
    private readonly Dictionary<Creature, bool> _cardDrawn = new();

    public override decimal ModifyEnergyGain(Player player, decimal amount)
    {
        if (amount <= 0) return amount;
        if (player == Owner.Player) return amount;
        if (player.Creature.Side != Owner.Side) return amount;

        _energyGained[player.Creature] = true;
        TryGrantJustice(player.Creature);
        return amount;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner == Owner.Player) return;
        if (card.Owner.Creature.Side != Owner.Side) return;

        _cardDrawn[card.Owner.Creature] = true;
        await TryGrantJusticeAsync(choiceContext, card.Owner.Creature);
    }

    private void TryGrantJustice(Creature target)
    {
        var hasEnergy = _energyGained.TryGetValue(target, out var e) && e;
        var hasDrawn = _cardDrawn.TryGetValue(target, out var d) && d;
        if (!hasEnergy || !hasDrawn) return;

        _energyGained[target] = false;
        _cardDrawn[target] = false;

        TaskHelper.RunSafely(PowerCmd.Apply<JusticePower>(
            null, target, 1m, Owner, null, false));
    }

    private async Task TryGrantJusticeAsync(PlayerChoiceContext choiceContext, Creature target)
    {
        var hasEnergy = _energyGained.TryGetValue(target, out var e) && e;
        var hasDrawn = _cardDrawn.TryGetValue(target, out var d) && d;
        if (!hasEnergy || !hasDrawn) return;

        _energyGained[target] = false;
        _cardDrawn[target] = false;

        await PowerCmd.Apply<JusticePower>(
            choiceContext, target, 1m, Owner, null, false);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
