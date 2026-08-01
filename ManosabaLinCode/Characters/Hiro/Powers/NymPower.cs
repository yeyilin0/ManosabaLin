using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class NymPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _isProcessing;
    private readonly HashSet<CardModel> _cardSourcesPendingDecrement = [];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!props.IsPoweredAttack()) return;
        if (result.TotalDamage <= 0) return;
        if (_isProcessing) return;

        Flash();
        _isProcessing = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner,
                result.TotalDamage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                dealer,
                cardSource,
                null);
        }
        finally
        {
            _isProcessing = false;
        }

        if (cardSource is null)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        _cardSourcesPendingDecrement.Add(cardSource);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_cardSourcesPendingDecrement.Remove(cardPlay.Card)) return;

        await PowerCmd.Decrement(this);
    }
}
