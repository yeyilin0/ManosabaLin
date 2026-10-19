using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Yalisalin.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ManosabaLin.Patches;

/// <summary>
///     「强化」后的【夺火】(<c>UsurpTheFlame.Strengthened</c>) 在<b>原有手牌描边之外</b>
///     再额外叠加一层 <c>#ffcc44</c> 色的加色边框光（<c>blend_add</c>）。
///     <para>
///         ⚠️ 只「加一层光」，**不动**原描边 —— 原描边颜色由 RitsuLib 的
///         <c>CardHandOutline</c>（见 <c>MainFile</c>，亚里沙拥有者 = 其角色色）给出，两者叠加。
///     </para>
///     <para>
///         与 <see cref="LyqinjinDogEarFramePatch" /> 同一套「往 <c>NCard.CardVfxContainer</c> 里塞
///         一个带 shader 的 <c>ColorRect</c>」做法，只是换成夺火的黄色光、且用独立的节点名避免冲突。
///     </para>
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals),
    [typeof(PileType), typeof(CardPreviewMode)])]
internal static class UsurpTheFlameStrengthenedGlowPatch
{
    private const string GlowNodeName = "ManosabaLinUsurpStrengthenedGlowOverlay";

    // 与 LyqinjinDogEarFramePatch 的 FrameGlow 用同一组卡面矩形（居中锚点 + 偏移）。
    private const float GlowLeft = -162f;
    private const float GlowTop = -226f;
    private const float GlowRight = 162f;
    private const float GlowBottom = 222f;

    private static readonly Lazy<Shader> GlowShader = new(CreateGlowShader);
    private static readonly Lazy<ShaderMaterial> GlowMaterial = new(CreateGlowMaterial);

    private static void Postfix(NCard __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) ||
            __instance.CardVfxContainer is not Control parent)
        {
            return;
        }

        var overlay = parent.GetNodeOrNull<ColorRect>(GlowNodeName);

        // 只有「强化后的夺火」才加这层光；其余一律隐藏（不清节点，留着复用）。
        if (__instance.Model is not UsurpTheFlame { Strengthened: true })
        {
            if (overlay is not null)
                overlay.Visible = false;
            return;
        }

        overlay ??= CreateOverlay(parent);
        ConfigureOverlay(overlay);
        overlay.Material = GlowMaterial.Value;
        overlay.Visible = true;
    }

    private static ColorRect CreateOverlay(Control parent)
    {
        var overlay = new ColorRect
        {
            Name = GlowNodeName,
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = GlowMaterial.Value,
        };

        parent.AddChild(overlay);
        return overlay;
    }

    private static void ConfigureOverlay(Control overlay)
    {
        overlay.AnchorLeft = 0.5f;
        overlay.AnchorRight = 0.5f;
        overlay.AnchorTop = 0.5f;
        overlay.AnchorBottom = 0.5f;
        overlay.OffsetLeft = GlowLeft;
        overlay.OffsetTop = GlowTop;
        overlay.OffsetRight = GlowRight;
        overlay.OffsetBottom = GlowBottom;
        overlay.CustomMinimumSize = new Vector2(GlowRight - GlowLeft, GlowBottom - GlowTop);
    }

    private static ShaderMaterial CreateGlowMaterial()
    {
        var material = new ShaderMaterial { Shader = GlowShader.Value };
        material.SetShaderParameter("primary_color", new Color("#ffcc44"));
        material.SetShaderParameter("secondary_color", new Color("#fff0b8"));
        return material;
    }

    private static Shader CreateGlowShader()
    {
        return new Shader
        {
            Code = """
shader_type canvas_item;
render_mode blend_add;

uniform vec4 primary_color : source_color = vec4(1.0, 0.80, 0.27, 1.0);
uniform vec4 secondary_color : source_color = vec4(1.0, 0.94, 0.72, 1.0);

float sd_rounded_box(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + vec2(r);
    return length(max(q, vec2(0.0))) + min(max(q.x, q.y), 0.0) - r;
}

void fragment() {
    vec4 inherited_color = COLOR;
    vec2 p = UV * 2.0 - vec2(1.0);
    p.x *= 0.74;

    float d = sd_rounded_box(p, vec2(0.68, 0.88), 0.13);
    float sharp_edge = 1.0 - smoothstep(0.010, 0.026, abs(d));
    float soft_glow = (1.0 - smoothstep(0.010, 0.170, abs(d))) * 0.42;
    float corner_weight = smoothstep(0.30, 0.95, abs(p.y));
    float alpha = sharp_edge * 0.62 + soft_glow * corner_weight;

    float flow = fract(UV.x * 0.70 + UV.y * 0.32 + TIME * 0.22);
    vec3 color = mix(primary_color.rgb, secondary_color.rgb, smoothstep(0.20, 0.92, flow));
    COLOR = vec4(color * inherited_color.rgb, clamp(alpha, 0.0, 0.80) * inherited_color.a);
}
""",
        };
    }
}
