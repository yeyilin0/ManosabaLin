using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class NymPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _isProcessing;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.TotalDamage <= 0) return;
        if (_isProcessing) return; // 防止无限循环

        Flash();

        _isProcessing = true;

        // 再受到一次等于伤害数值的伤害（不受格挡、不受力量影响）
        await CreatureCmd.Damage(
            choiceContext,
            Owner,
            result.TotalDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            dealer,
            cardSource
        );

        _isProcessing = false;

        // 清除一层
        await PowerCmd.Decrement(this);
    }
}