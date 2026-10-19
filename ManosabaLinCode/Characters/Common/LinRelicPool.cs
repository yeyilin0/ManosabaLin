using Godot;
using ManosabaLin.Extensions;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

/// <summary>
///     本 mod 的共享遗物池（「Lin 池」）。
///     <para>
///         12 个海克斯联动遗物注册在这里（<c>[RegisterRelic(typeof(LinRelicPool))]</c>）。
///         它不绑定任何角色，登记为 <b>共享遗物池</b> ⇒ 进入 <c>ModelDb.AllSharedRelicPools</c>。
///         与 <c>LinCardPool</c>（<c>[RegisterSharedCardPool]</c>）同构。
///     </para>
///     <para>
///         ⚠️ <b>「角色专属」不靠引擎的角色遗物池表达</b>（2026-10-15 用户裁定：曾短暂改挂角色池，
///         改回本池）。海克斯符文是不是「角色专属」由<b>海克斯侧</b>决定：本模组经
///         <c>SetPlayerRuneCharacter</c> 把符文登记到专属我方角色（进角色专属档 + 150% 加权），
///         各字段的可用性过滤（按角色收窄的 <c>isAvailableForPlayer</c>）进一步收窄出现条件。
///         引擎侧池成员只影响原版图鉴的「角色遗物 / 通用」归类，与本联动的专属语义无关。
///     </para>
///     <para>
///         海克斯联动遗物装了海克斯时仍会**额外**通过
///         <c>HextechRunesInterop.RegisterPlayerRune</c> 拿到「海克斯符文」的第二身份
///         （软依赖；注册/绑定零 patch，另有一个只作用于图鉴分区的显示层补丁，见上）。
///     </para>
/// </summary>
[RegisterSharedRelicPool]
public class LinRelicPool : TypeListRelicPoolModel
{
    private const string CharacterIdLower = "linenergy";

    public override string EnergyColorName => CharacterIdLower;

    public override Color LabOutlineColor => new("B72222");

    public override string BigEnergyIconPath => "characters/Hiro/linenergy.png".ImagePath();

    public override string TextEnergyIconPath => "characters/Hiro/linenergy_small.png".ImagePath();
}
