using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Common.AncientCurses;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Sherrylin.Cards;
using ManosabaLin.Characters.Yalisalin.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ManosabaLin.Patches;

/// <summary>
/// 13 张原罪诅咒卡名描边色映射表。
/// 颜色取自 zhs/cards.json 各卡 description 的主题色
/// （description 里第一处 [color=#xxxxxx] 或 [gold]/[purple]/[green]/[blue] 标签）。
/// 卡名文字保持米白（StsColors.cream #FFF6E2），只替换描边色。
/// </summary>
internal static class LinAncientCurseTitleColors
{
    private static readonly Dictionary<Type, Color> ByCardType = new()
    {
        // 希罗的偏执：{CompPre}[color=#CC6666]
        [typeof(Hiroparanoid)] = new("#CC6666"),
        // 夏目安安的虚妄：[color=#6666cc]
        [typeof(AnanlinVanity)] = new("#6666CC"),
        // 佐伯米莉亚的迷失：[color=#cc9966]
        [typeof(MiliaLost)] = new("#CC9966"),
        // 冰上梅露露的怯懦：[color=#ffcc99]
        [typeof(MeruruCowardice)] = new("#FFCC99"),
        // 樱羽艾玛的悔恨：[color=#ff99cc]
        [typeof(Emaregret)] = new("#FF99CC"),
        // 橘雪莉的空洞：[color=#33ccff]
        [typeof(SherryVoid)] = new("#33CCFF"),
        // 莲见蕾雅的痴狂：[gold] -> StsColors.gold #EFC851
        [typeof(RaiyaMadness)] = new("#EFC851"),
        // 宝生玛格的惑情：[purple] -> StsColors.purple #EE82EE
        [typeof(MargeCharm)] = new("#EE82EE"),
        // 黑部奈叶香的猜忌：[color=#999999]
        [typeof(NayukaJealousy)] = new("#999999"),
        // 远野汉娜的狂想：[green] -> StsColors.green #7FFF00
        [typeof(Hannadelusion)] = new("#7FFF00"),
        // 城崎诺亚的裹挟：[blue] -> StsColors.blue #87CEEB
        [typeof(NoahEnsnare)] = new("#87CEEB"),
        // 紫藤亚里沙的负疚：[color=#ff0000]
        [typeof(ArisaGuilt)] = new("#FF0000"),
        // 泽度可可的忧泯：[color=#ff9966]
        [typeof(Cocoworry)] = new("#FF9966"),
        // 魔女化诅咒卡：白字灰描边（用户指定 #cccccc）
        [typeof(WitchificationCurse)] = new("#CCCCCC"),
    };

    /// <summary>按卡的具体类型取描边色；非原罪诅咒返回 false。</summary>
    public static bool TryGetOutlineColor(CardModel? model, out Color color)
    {
        if (model != null && ByCardType.TryGetValue(model.GetType(), out color))
            return true;
        color = default;
        return false;
    }
}

/// <summary>
/// 原罪诅咒（继承 <see cref="LinAncientCurseCard"/> 的 13 张卡）视觉补丁：
/// 稀有度数据改为普通诅咒（Curse）后，卡框仍显示先古（Ancient）卡框，稀有度不变。
/// 挂载点与原版 Ancient 卡一致：在 NCard.Reload() 之后执行。
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.Reload))]
[HarmonyPriority(Priority.Low)]
public static class LinAncientCurseFramePatch
{
    private const string AncientBorderPath = "res://images/atlases/compressed.sprites/card_template/ancient_card_border.tres";

    // 先古文字底纹理路径模板：与原版 CardModel.AncientTextBg 的取法一致，按卡牌类型取对应变体
    // （原版规则：None / Status / Curse 一律映射为 Skill，其余用自身类型）
    private const string AncientTextBgPathFormat = "res://images/atlases/compressed.sprites/card_template/ancient_card_text_bg_{0}.tres";

    private static string AncientTextBgPath(CardModel? model) => string.Format(
        AncientTextBgPathFormat,
        model?.Type switch
        {
            CardType.Attack => "attack",
            CardType.Power => "power",
            _ => "skill",
        });

    private const string CanvasGroupMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";

    static void Postfix(NCard __instance)
    {
        var container = __instance.GetNode<Control>("CardContainer");
        if (container == null) return;

        // 只处理 13 张原罪诅咒（统一继承 LinAncientCurseCard，含二阶堂希罗的偏执）
        // 以及各角色的初始卡「精神创伤」（同样套用先古卡面）
        // 以及紫藤亚里沙的爆裂魔法
        // 以及角色支援/联动卡：安安的米莉亚支援与诺亚支援、雪莉的艾玛希儿与汉娜支援
        // 以及希罗的「协力」卡：艾玛的协力（Amhelp）、蕾雅的协力（Lyhelp）
        // 以及「已有卡图」的多人专属卡（排除各角色的「佐伯米莉亚」Mlym 系列）
        bool isOurCard = __instance.Model is LinAncientCurseCard
            or YalisalinMentalTrauma or MentalTrauma or EmalinMentalTrauma
            or AnanlinMentalTrauma or SherrylinMentalTrauma or ExplosionMagic
            or AnanlinMiliaAssist or AnanlinNoahAssist or Emaxieli or HannaAssist
            or Amhelp or Lyhelp or Xhelp or NymSlash
            or AnanlinConceivingNovelist or AnanlinGroupChatSmell or AnanlinTakeTheHitForTeammate
            or AnanlinFeelingAtEase or AnanlinPageStrike
            or AnanlinSaveLoad or AnanlinMiriasMoviePromise
            or BondVerdict or EnchantmentConvergence or SuspectCovenant
            or FlowerBloom or JointJudgment or BondSettlement or BondExchangecard
            or Geinilunhui or JusticeRelay or SharedWitchRitual
            or IAmCorrect or InstantTransmigration or CardEightySix or Threehiro
            or SharedExhaustPlay or TeamZeroGift or ThreeCardOmen
            or EmotionTransfer or FriendshipSeeking or LastCase
            or CombustionShared or StokeForYou or TorchPassing or SharedGuilt or EmberDividend
            or AgeOfFire or BoundPrometheus or FinalFarewell;

        if (!isOurCard)
        {
            // 非原罪诅咒：若它带 AncientBanner/Fire（原版真先古卡），恢复火焰可见，
            // 防止本补丁的可见性切换影响原版先古卡
            if (container.HasNode("AncientBanner/Fire") && container.GetNode("AncientBanner/Fire") is CanvasItem restoredFire)
                restoredFire.Visible = true;
            return;
        }

        // 卡名主题色描边：保持米白卡名（cream），按卡各自的主题色设置描边
        // （UpdateTitleLabel 可能在 Reload 之后再次设置描边色，这里先设一次兜底；
        //   真正的防覆盖由 LinAncientCurseTitlePatch 的 Postfix 保证）
        if (LinAncientCurseTitleColors.TryGetOutlineColor(__instance.Model, out var outlineColor) &&
            __instance.GetNodeOrNull<Label>("%TitleLabel") is { } titleLabel)
            titleLabel.AddThemeColorOverride("font_outline_color", outlineColor);

        // 可见性切换

        // 隐藏普通框节点
        foreach (var name in new[] { "PortraitBorder", "TitleBanner", "StarIcon" })
        {
            if (container.HasNode(name) && container.GetNode(name) is CanvasItem item)
                item.Visible = false;
        }

        // 显示先古框节点
        foreach (var name in new[] { "AncientTextBg", "AncientBanner" })
        {
            if (container.HasNode(name) && container.GetNode(name) is CanvasItem item)
                item.Visible = true;
        }

        // 隐藏 Fire 子节点（火焰动画只属于真先古卡）
        if (container.HasNode("AncientBanner/Fire") && container.GetNode("AncientBanner/Fire") is CanvasItem fire)
            fire.Visible = false;

        bool hasAncientBorder = container.HasNode("AncientBorder");
        if (hasAncientBorder)
        {
            // 完整场景：显示 AncientBorder，隐藏普通 Frame
            if (container.GetNode("AncientBorder") is TextureRect ancientBorderRect)
            {
                var ancientBorderTex = ResourceLoader.Load<Texture2D>(AncientBorderPath, null, ResourceLoader.CacheMode.Reuse);
                if (ancientBorderTex != null)
                    ancientBorderRect.Texture = ancientBorderTex;
                ancientBorderRect.Visible = true;
            }
            if (container.HasNode("Frame") && container.GetNode("Frame") is CanvasItem frame)
                frame.Visible = false;
        }
        else
        {
            // 精简场景：复用 Frame 节点，替换为 Ancient 边框纹理
            if (container.HasNode("Frame") && container.GetNode("Frame") is TextureRect frame)
            {
                var ancientBorderTex = ResourceLoader.Load<Texture2D>(AncientBorderPath, null, ResourceLoader.CacheMode.Reuse);
                if (ancientBorderTex != null)
                    frame.Texture = ancientBorderTex;
                frame.Visible = true;
            }
        }

        // 显示 AncientHighlight（原版 Ancient 卡的装饰性金色光晕；当前场景无此节点时自然跳过）
        if (container.HasNode("AncientHighlight") && container.GetNode("AncientHighlight") is CanvasItem ancientHL)
            ancientHL.Visible = true;

        // 不能隐藏 Highlight（NCardHighlight），它是选中/悬停高亮的唯一提供者
        // 原版 NCard.Reload() 在处理 Ancient 卡时也不碰 Highlight 的可见性

        // 纹理设置
        // model.AncientTextBg 内部检查 Rarity==Ancient 才返回，非 Ancient 卡会抛异常
        // 需要手动按卡牌类型拼路径加载先古文字底纹理
        if (container.HasNode("AncientTextBg") && container.GetNode("AncientTextBg") is TextureRect ancientTextBg)
        {
            var tex = ResourceLoader.Load<Texture2D>(AncientTextBgPath(__instance.Model), null, ResourceLoader.CacheMode.Reuse);
            if (tex != null)
                ancientTextBg.Texture = tex;
        }

        // 遮罩裁剪
        // 原版 Ancient 卡通过 mask material 裁剪 AncientPortrait 溢出部分
        // 路径来自 NCard.Reload() 中的 _canvasGroupMaskMaterialPath
        var portraitCanvasGroup = container.GetNodeOrNull<CanvasGroup>("PortraitCanvasGroup");
        if (portraitCanvasGroup != null)
        {
            var maskMat = ResourceLoader.Load<Material>(CanvasGroupMaskMaterialPath, null, ResourceLoader.CacheMode.Reuse);
            if (maskMat != null)
                portraitCanvasGroup.Material = maskMat;
        }

        // Portrait 处理

        // 隐藏普通 Portrait
        if (portraitCanvasGroup?.HasNode("Portrait") == true && portraitCanvasGroup.GetNode("Portrait") is CanvasItem normalPortrait)
            normalPortrait.Visible = false;

        // 显示 AncientPortrait 并设置纹理
        if (portraitCanvasGroup?.HasNode("AncientPortrait") == true && portraitCanvasGroup.GetNode("AncientPortrait") is TextureRect ancientPortrait)
        {
            var model = __instance.Model;
            if (model?.Portrait != null)
            {
                ancientPortrait.Texture = model.Portrait;
                ancientPortrait.Modulate = Colors.White;
            }
            ancientPortrait.Visible = true;
        }

        // 绘制顺序：本游戏版本的 card.tscn 中 AncientTextBg / OverlayContainer 的层级
        // 已与原版 Ancient 卡的渲染顺序一致，无需 MoveChild 调整。
    }
}

/// <summary>
/// 原罪诅咒卡名描边补丁：NCard.UpdateTitleLabel 按稀有度设置标题颜色，
/// Curse 会写入紫色描边（cardTitleOutlineCurse），这里在之后覆盖为每张卡的主题色，
/// 保证任何时机（手牌、奖励、图鉴展示）卡名都是白字 + 各自描边色。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateTitleLabel")]
internal static class LinAncientCurseTitlePatch
{
    static void Postfix(NCard __instance)
    {
        if (__instance.Model is not LinAncientCurseCard) return;
        if (LinAncientCurseTitleColors.TryGetOutlineColor(__instance.Model, out var outlineColor) &&
            __instance.GetNodeOrNull<Label>("%TitleLabel") is { } titleLabel)
            titleLabel.AddThemeColorOverride("font_outline_color", outlineColor);
    }
}

/// <summary>
/// 原罪诅咒类型徽章补丁：类型小正方形（TypePlaque，显示"诅咒"的小方块）
/// 原版用 hsv.gdshader 材质（curse 参数 h=0.27, s=1.1, v=0.9）把底图调成紫色，
/// 这里替换为固定 #cccccc 浅灰的纯色材质（保留底图 alpha 形状），
/// TypeLabel 黑色"诅咒"字样保持场景默认不变。
/// UpdateTypePlaque 每次展示都会重设材质，Postfix 保证始终覆盖为先古灰底。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdateTypePlaque")]
internal static class LinAncientCurseTypePlaquePatch
{
    // #cccccc
    private const float PlaqueGray = 0.8f;

    private static readonly Lazy<ShaderMaterial> PlaqueMaterial = new(CreatePlaqueMaterial);

    private static ShaderMaterial CreatePlaqueMaterial()
    {
        return new ShaderMaterial
        {
            Shader = new Shader
            {
                Code = """
shader_type canvas_item;
render_mode unshaded;

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    COLOR = vec4(vec3(0.8), tex.a);
}
"""
            }
        };
    }

    static void Postfix(NCard __instance)
    {
        if (__instance.Model is not LinAncientCurseCard) return;
        if (__instance.GetNodeOrNull<NinePatchRect>("%TypePlaque") is { } plaque)
            plaque.Material = PlaqueMaterial.Value;
    }
}

