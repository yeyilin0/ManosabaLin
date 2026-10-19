using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves.Runs;

using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     痛觉的重量（独立能力）：
///     每通过余火烧掉 <see cref="Threshold" /> 张牌，本场获得 <c>Amount</c> 点力量。
///     阈值由卡牌打出时写入（普通 2 / 升级 1）；重复获得时保留更低的阈值。
/// </summary>
[RegisterPower]
public sealed class WeightOfPainPower : ManosabaPowerTemplate
{
    private const int DefaultThreshold = 2;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    [SavedProperty] public int BurnProgress { get; private set; }
    [SavedProperty] public int Threshold { get; private set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar("Threshold", DefaultThreshold)];

    /// <summary>
    ///     描述随「阈值是否已被升级牌降到 1」切键（升级也是一种达成条件）。数值本身由 <c>{Threshold}</c>
    ///     动态变量给出，这里只按状态切键。两侧通道都切，做法对齐「共犯」/ 被缚的普罗米修斯。
    /// </summary>
    public override LocString Description
    {
        get
        {
            var description = new LocString("powers",
                CurrentThreshold <= 1 ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");
            description.Add(new IntVar("Threshold", CurrentThreshold));
            return description;
        }
    }

    protected override string SmartDescriptionLocKey =>
        CurrentThreshold <= 1 ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";

    private int CurrentThreshold => Threshold <= 0 ? DefaultThreshold : Threshold;

    public void ConfigureThreshold(int threshold)
    {
        var normalized = Math.Max(1, threshold);
        Threshold = Threshold <= 0 ? normalized : Math.Min(Threshold, normalized);
    }

    /// <summary>
    ///     由遗物的余火「烧掉牌后」钩子转发调用：累计烧牌数，达到阈值时给出 <c>Amount</c> 点力量。
    /// </summary>
    public async Task ResolveBurned(PlayerChoiceContext choiceContext)
    {
        var owner = Owner;
        if (owner is null || Amount <= 0)
            return;

        var threshold = CurrentThreshold;
        BurnProgress++;
        if (BurnProgress < threshold)
            return;

        BurnProgress -= threshold;
        await PowerCmd.Apply<StrengthPower>(choiceContext, owner, Amount, owner, null, false);
    }
}
