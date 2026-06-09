using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 无法出牌能力：拥有此能力时无法打出卡牌。
/// </summary>
[RegisterPower]
public sealed class CannotPlayCardsPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.None;
}
