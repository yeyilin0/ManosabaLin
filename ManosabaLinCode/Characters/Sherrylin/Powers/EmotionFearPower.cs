using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// Fear能力：效果已移至充能球。此能力仅用于球体生命周期管理。
/// </summary>
[RegisterPower]
public sealed class EmotionFearPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
}
