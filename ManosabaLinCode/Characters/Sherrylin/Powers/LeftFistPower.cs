// LeftFistPower.cs
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class LeftFistPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner || !props.IsPoweredAttack()) return;
        await PowerCmd.Apply<TempStrength>(choiceContext, Owner, 1, Owner, null, false);
    }
}