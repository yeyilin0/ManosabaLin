using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Loseengry : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _drawCount;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        Flash();
        await PlayerCmd.LoseEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != Owner) return;

        _drawCount++;
        if (_drawCount >= 4)
        {
            _drawCount = 0;

            if (Amount <= 1)
            {
                await RemoveAndTrigger(choiceContext);
            }
            else
            {
                SetAmount(Amount - 1);
            }
        }
    }

    private async Task RemoveAndTrigger(PlayerChoiceContext choiceContext)
    {
        int maxEnergy = Owner.Player.PlayerCombatState.MaxEnergy;
        var orbs = Owner.Player.PlayerCombatState.OrbQueue.Orbs;
        bool hasOrb = orbs.Any(o => o is EmotionElationOrb);

        await PowerCmd.Remove(this);

        if (hasOrb)
        {
            await PlayerCmd.GainEnergy(maxEnergy, Owner.Player);
            await PowerCmd.Apply<Loseengry>(
                choiceContext, Owner, maxEnergy,
                Owner, null, false);
        }
    }
}
