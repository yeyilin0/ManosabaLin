using System.Text.Json.Nodes;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Hiro.Capabilities;

/// <summary>
///     「轮回配对」标记（RitsuLib 能力 / model-backed component，**隐藏**）：
///     由 <c>CardSeventyNine</c>（宿命的轮替）升级后，只挂在**「获得轮回」的那些卡**上，
///     使【轮回】能认出它们是一对 —— 无论打出其中哪一张，都按**同一个卡名**触发同名规则。
///     <para>
///         ⭐ <see cref="PairNames" /> ＝ 这一对的**卡名来源**，即「第 2 次选择」那张
///         （第一张获得轮回的卡）的卡名（可多个：一张卡被多次配对）。
///         两张卡的该列表只要有交集 ⇒ 视为「同名」。
///     </para>
///     <para>
///         ⚠️ **「失去轮回」的那张卡（第 1 次选择）不挂本能力**，在配对这件事上没有任何效果；
///         第 2 次选择的那张自己**不改名**，只有第 3 次选择的那张会被改名为它的卡名。
///     </para>
///     <para>
///         ⭐ **隐藏**：不写任何本地化键（<c>beforeBase</c>/<c>afterBase</c>/<c>hovertip.*</c> 都不存在），
///         基类 <see cref="ManosabaCardCapability" /> 取不到文本 ⇒ 卡面描述与悬浮里都看不到它。
///         玩家看到的只有「第 3 次选择那张的卡名显示成了第 2 次那张的卡名」（<c>CardRename</c> 负责，纯视觉）。
///     </para>
///     <para>
///         ⚠️ 状态走 RitsuLib 的 <c>SaveAdditionalState</c>/<c>LoadAdditionalState</c>，
///         随卡牌实例一起存档（<c>MarkDirty()</c> 后能力集合才会被标记为需要保存）。
///         ⚠️ 战斗中的卡是牌组（<c>PileType.Deck</c>）卡的**克隆**，所以本标记天然只作用于本场战斗。
///     </para>
/// </summary>
[RegisterModelCapability]
public sealed class TransmigrationPairCapability : ManosabaCardCapability
{
    private const string PairNamesKey = "pairNames";

    private readonly List<string> _pairNames = [];

    /// <summary>这张卡所属的全部配对名（＝各次配对时「第 2 次选择」那张卡的卡名）。</summary>
    public IReadOnlyList<string> PairNames => _pairNames;

    /// <summary>
    ///     追加一个配对名（已在列表里则忽略）。非空且真的新增时标记为脏，让能力集合进入存档。
    /// </summary>
    public void AddPairName(string pairName)
    {
        if (string.IsNullOrEmpty(pairName)) return;
        if (_pairNames.Contains(pairName)) return;

        _pairNames.Add(pairName);
        MarkDirty();
    }

    /// <summary>
    ///     两张卡是否由同一次「宿命的轮替」（或同一条配对链）结成一对：
    ///     双方都带本能力，且 <see cref="PairNames" /> 有交集。
    /// </summary>
    internal static bool AreLinked(CardModel? a, CardModel? b)
    {
        if (a is null || b is null || ReferenceEquals(a, b))
            return false;

        if (!a.TryGetCapability<TransmigrationPairCapability>(out var capA)) return false;
        if (!b.TryGetCapability<TransmigrationPairCapability>(out var capB)) return false;

        foreach (var self in capA._pairNames)
            if (self.Length > 0 && capB._pairNames.Contains(self))
                return true;

        return false;
    }

    protected override JsonNode? SaveAdditionalState()
    {
        var pairNames = new JsonArray();
        foreach (var pairName in _pairNames)
            pairNames.Add(pairName);

        return new JsonObject
        {
            [PairNamesKey] = pairNames
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        _pairNames.Clear();

        if (state?[PairNamesKey] is not JsonArray array) return;

        foreach (var node in array)
            if (node?.GetValue<string>() is { Length: > 0 } pairName)
                _pairNames.Add(pairName);
    }
}
