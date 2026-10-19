using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Common.AncientCurses;

/// <summary>
/// 特殊原罪诅咒卡基类：Lin 卡池、普通诅咒稀有度、诅咒类型、图鉴可见（默认 true）。
/// 卡框视觉（先古金框）由
/// <see cref="ManosabaLin.Patches.LinAncientCurseFramePatch"/> 提供，
/// 稀有度数据本身保持 Curse，与普通诅咒卡一致。
/// <para>
///     <b>不自带 <see cref="Originalsin" /> 组件</b>：裸卡就是一张纯诅咒，
///     「原罪」关键词只在亚里沙拿到它们时由调用方挂载
///     （<see cref="AncientSinCardCatalog" /> 的注释与各获得卡一致）。
/// </para>
/// </summary>
public abstract class LinAncientCurseCard(int energyCost = 2, TargetType targetType = TargetType.None)
    : ManosabaCardTemplate(energyCost, CardType.Curse, CardRarity.Curse, targetType, shouldShowInCardLibrary: true)
{
    public override int MaxUpgradeLevel => 0;
}
