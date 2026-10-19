using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Common.HiroKeywords;

/// <summary>
///     「卡名覆盖」登记表：让一张卡在结算中临时<b>显示成另一个名字</b>（效果完全不变）。
///     <para>
///         ⭐ 用途：<c>CardSeventyNine</c>（宿命的轮替）升级后「使后一张卡名改为前一张卡卡名」。
///         ⚠️ 这只是**视觉**：真正的「两张卡算同名」由 <c>TransmigrationPairCapability</c>
///         这个隐藏能力承担（见 <c>TransmigrationRules</c>）—— 这里只负责玩家看到的名字。
///     </para>
///     <para>
///         ⚠️ 引擎没有「运行时改卡名」的原生 API（<c>CardModel.Title</c> 是从 <c>Id.Entry</c> 的本地化键
///         现算的）⇒ 只能由 <c>Patches/CardRenameTitlePatch.cs</c> 在 <c>get_Title</c> 上兜一层。
///     </para>
///     <para>
///         ⚠️ 用 <see cref="ConditionalWeakTable{TKey,TValue}" />：卡实例被丢弃时登记随之消失，不留泄漏。
///         这是**战斗内**的临时状态（不存档）；若该卡实例被克隆（如附魔时的 <c>MutableClone</c>），
///         改名不会跟着克隆 —— 对本卡用途（手牌里的卡稍后被弃/打出）无影响。
///     </para>
/// </summary>
internal static class CardRename
{
    private static readonly ConditionalWeakTable<CardModel, object> Names = new();

    private sealed class Box(string value)
    {
        internal string Value { get; } = value;
    }

    /// <summary>把这张卡的名字覆盖为 <paramref name="name" />。</summary>
    internal static void Set(CardModel card, string name)
    {
        Names.Remove(card);
        Names.Add(card, new Box(name));
    }

    /// <summary>取这张卡被覆盖的名字（没有则 false）。</summary>
    internal static bool TryGet(CardModel card, out string name)
    {
        if (Names.TryGetValue(card, out var box) && box is Box { Value: { Length: > 0 } value })
        {
            name = value;
            return true;
        }

        name = string.Empty;
        return false;
    }

    /// <summary>
    ///     卡的<b>有效卡名</b>（改过名就取改后的，否则取本地化原名 —— 刻意用
    ///     <c>TitleLocString.GetFormattedText()</c> 而不是 <c>Title</c>，后者对升级卡会带上「+」后缀，
    ///     会让「打击」与「打击+」被判成不同名）。
    /// </summary>
    internal static string EffectiveName(CardModel card)
        => TryGet(card, out var name) ? name : card.TitleLocString.GetFormattedText();

    /// <summary>
    ///     两张卡的<b>显示名</b>是否相同（同 Id，或有效卡名相同）。
    ///     ⚠️ 只描述「玩家看到的名字」；【轮回】的配对判据**不再用它**，
    ///     见 <c>TransmigrationRules.IsSameNameForTransmigration</c> 与
    ///     <c>TransmigrationPairCapability</c>。
    /// </summary>
    internal static bool SameName(CardModel a, CardModel b)
        => a.Id == b.Id || EffectiveName(a) == EffectiveName(b);
}
