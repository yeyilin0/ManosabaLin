using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Reviveritualpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _revivePending;

    public void SetRevivePending(bool value) => _revivePending = value;

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner) return true;
        if (_revivePending) return false;
        return true;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner?.Creature != Owner) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();

        await PowerCmd.Apply<BufferPower>(
            new ThrowingPlayerChoiceContext(), creature, 999m, creature, null, false);

        _revivePending = false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        if (Owner.CurrentHp >= Owner.MaxHp)
        {
            var buffer = Owner.GetPower<BufferPower>();
            if (buffer != null)
                await PowerCmd.Remove(buffer);

            await PowerCmd.Remove(this);
            return;
        }

        if (Amount <= 1)
        {
            var buffer = Owner.GetPower<BufferPower>();
            if (buffer != null)
                await PowerCmd.Remove(buffer);

            await CreatureCmd.Damage(
                choiceContext, Owner, Owner.MaxHp,
                MegaCrit.Sts2.Core.ValueProps.ValueProp.Unblockable, Owner, null);
            await PowerCmd.Remove(this);
        }
        else
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null, false);
        }
    }
}