using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals),
    [typeof(PileType), typeof(CardPreviewMode)])]
internal static class LyqinjinDogEarFramePatch
{
    private enum BondEarStyle
    {
        None,
        Affinity,
        Estrangement,
        BadEnding,
        TrueEnding,
        YalisaQinjin,
        PinkGray
    }

    private enum BondFrameGlowStyle
    {
        None,
        Affinity,
        Estrangement
    }

    private const string DogEarNodeName = "ManosabaLinAffinityDogEarOverlay";
    private const string FrameGlowNodeName = "ManosabaLinAffinityFrameGlowOverlay";
    private const float EarOverlayLeft = -150f;
    private const float EarOverlayTop = -302f;
    private const float EarOverlayRight = 150f;
    private const float EarOverlayBottom = -206f;
    private const float FrameGlowLeft = -162f;
    private const float FrameGlowTop = -226f;
    private const float FrameGlowRight = 162f;
    private const float FrameGlowBottom = 222f;

    private static readonly Lazy<Shader> DogEarShader = new(CreateDogEarShader);
    private static readonly Lazy<Shader> FrameGlowShader = new(CreateFrameGlowShader);

    private static readonly Lazy<ShaderMaterial> NormalAffinityDogEarMaterial = new(
        () => CreateDogEarMaterial(
            false,
            new Color(1f, 0.58f, 0.82f, 1f),
            new Color(0.20f, 0.05f, 0.13f, 1f),
            new Color(1f, 0.95f, 1f, 1f)));

    private static readonly Lazy<ShaderMaterial> ActiveAffinityDogEarMaterial = new(
        () => CreateDogEarMaterial(
            true,
            new Color(1f, 0.42f, 0.76f, 1f),
            new Color(0.78f, 0.24f, 0.54f, 1f),
            new Color(1f, 0.62f, 0.84f, 1f),
            1.5f));

    private static readonly Lazy<ShaderMaterial> NormalEstrangementCatEarMaterial = new(
        () => CreateDogEarMaterial(
            false,
            new Color(0.68f, 0.15f, 0.18f, 1f),
            new Color(0.08f, 0.01f, 0.02f, 1f),
            new Color(0.92f, 0.30f, 0.34f, 1f)));

    private static readonly Lazy<ShaderMaterial> ActiveEstrangementCatEarMaterial = new(
        () => CreateDogEarMaterial(
            true,
            new Color(0.72f, 0f, 0.04f, 1f),
            new Color(0.16f, 0f, 0.01f, 1f),
            new Color(0.26f, 0f, 0.02f, 1f),
            1.6f));

    private static readonly Lazy<ShaderMaterial> BadEndingDogEarMaterial = new(
        () => CreateSplitDogEarMaterial(
            true,
            new Color(0.72f, 0f, 0.04f, 1f),
            new Color(0.16f, 0f, 0.01f, 1f),
            new Color(0.26f, 0f, 0.02f, 1f),
            new Color(1f, 0.42f, 0.76f, 1f),
            new Color(0.78f, 0.24f, 0.54f, 1f),
            new Color(1f, 0.62f, 0.84f, 1f),
            1.55f));

    private static readonly Lazy<ShaderMaterial> TrueEndingDogEarMaterial = new(
        () => CreateSplitDogEarMaterial(
            true,
            new Color(1f, 0.42f, 0.76f, 1f),
            new Color(0.78f, 0.24f, 0.54f, 1f),
            new Color(1f, 0.62f, 0.84f, 1f),
            new Color(0.72f, 0f, 0.04f, 1f),
            new Color(0.16f, 0f, 0.01f, 1f),
            new Color(0.26f, 0f, 0.02f, 1f),
            1.55f));

    private static readonly Lazy<ShaderMaterial> YalisaQinjinDogEarMaterial = new(
        () => CreateDogEarMaterial(
            true,
            new Color(1f, 0.42f, 0.72f, 1f),
            new Color(0.70f, 0.10f, 0.40f, 1f),
            new Color(1f, 0.08f, 0.10f, 1f),
            1.55f));

    private static readonly Lazy<ShaderMaterial> PinkGrayDogEarMaterial = new(
        () => CreateDogEarMaterial(
            true,
            new Color(1f, 0.48f, 0.74f, 1f),
            new Color(0.48f, 0.40f, 0.46f, 1f),
            new Color(0.78f, 0.70f, 0.76f, 1f),
            1.35f));

    private static readonly Lazy<ShaderMaterial> AffinityFrameGlowMaterial = new(
        () => CreateFrameGlowMaterial(
            new Color(1f, 0.50f, 0.78f, 1f),
            new Color(1f, 0.96f, 1f, 1f)));

    private static readonly Lazy<ShaderMaterial> EstrangementFrameGlowMaterial = new(
        () => CreateFrameGlowMaterial(
            new Color(0.70f, 0f, 0.04f, 1f),
            new Color(0.16f, 0f, 0.01f, 1f)));

    private static void Postfix(NCard __instance)
    {
        if (!GodotObject.IsInstanceValid(__instance) ||
            __instance.CardVfxContainer is not Control parent)
        {
            return;
        }

        var dogEarOverlay = parent.GetNodeOrNull<ColorRect>(DogEarNodeName);
        var frameGlowOverlay = parent.GetNodeOrNull<ColorRect>(FrameGlowNodeName);
        var model = __instance.Model;
        var style = model is not null ? GetBondEarStyle(model) : BondEarStyle.None;
        var shouldShow = style != BondEarStyle.None;
        var dogEarActive = model is not null && shouldShow && ShouldUseActiveDogEarMaterial(model, style);
        var frameGlowStyle = model is not null && shouldShow
            ? GetFrameGlowStyle(model, style)
            : BondFrameGlowStyle.None;

        if (!shouldShow)
        {
            if (dogEarOverlay is not null)
                dogEarOverlay.Visible = false;
            if (frameGlowOverlay is not null)
                frameGlowOverlay.Visible = false;
            return;
        }

        if (frameGlowStyle != BondFrameGlowStyle.None)
        {
            frameGlowOverlay ??= CreateFrameGlowOverlay(parent);
            ConfigureFrameGlowOverlay(frameGlowOverlay);
            frameGlowOverlay.Material = GetFrameGlowMaterial(frameGlowStyle);
            frameGlowOverlay.Visible = true;
        }
        else if (frameGlowOverlay is not null)
        {
            frameGlowOverlay.Visible = false;
        }

        dogEarOverlay ??= CreateDogEarOverlay(parent);
        ConfigureDogEarOverlay(dogEarOverlay);
        dogEarOverlay.Material = GetDogEarMaterial(style, dogEarActive);
        dogEarOverlay.Visible = true;
        dogEarOverlay.MoveToFront();
    }

    private static ColorRect CreateDogEarOverlay(Control parent)
    {
        var overlay = new ColorRect
        {
            Name = DogEarNodeName,
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = NormalAffinityDogEarMaterial.Value,
        };

        parent.AddChild(overlay);
        return overlay;
    }

    private static ColorRect CreateFrameGlowOverlay(Control parent)
    {
        var overlay = new ColorRect
        {
            Name = FrameGlowNodeName,
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = AffinityFrameGlowMaterial.Value,
        };

        parent.AddChild(overlay);
        return overlay;
    }

    private static void ConfigureDogEarOverlay(Control overlay)
    {
        overlay.AnchorLeft = 0.5f;
        overlay.AnchorRight = 0.5f;
        overlay.AnchorTop = 0.5f;
        overlay.AnchorBottom = 0.5f;
        overlay.OffsetLeft = EarOverlayLeft;
        overlay.OffsetTop = EarOverlayTop;
        overlay.OffsetRight = EarOverlayRight;
        overlay.OffsetBottom = EarOverlayBottom;
        overlay.CustomMinimumSize = new Vector2(EarOverlayRight - EarOverlayLeft, EarOverlayBottom - EarOverlayTop);
    }

    private static void ConfigureFrameGlowOverlay(Control overlay)
    {
        overlay.AnchorLeft = 0.5f;
        overlay.AnchorRight = 0.5f;
        overlay.AnchorTop = 0.5f;
        overlay.AnchorBottom = 0.5f;
        overlay.OffsetLeft = FrameGlowLeft;
        overlay.OffsetTop = FrameGlowTop;
        overlay.OffsetRight = FrameGlowRight;
        overlay.OffsetBottom = FrameGlowBottom;
        overlay.CustomMinimumSize = new Vector2(FrameGlowRight - FrameGlowLeft, FrameGlowBottom - FrameGlowTop);
    }

    private static BondEarStyle GetBondEarStyle(CardModel card)
    {
        var cardType = card.GetType();
        if (cardType == typeof(EmaBadEnding))
            return BondEarStyle.BadEnding;

        if (cardType == typeof(EmaTrueEnding))
            return BondEarStyle.TrueEnding;

        if (cardType == typeof(Yalisaqinjin))
            return BondEarStyle.YalisaQinjin;

        if (cardType == typeof(BondExchangecard)
            || cardType == typeof(Xueshuyuancard))
            return BondEarStyle.PinkGray;

        if (cardType == typeof(Xueqinjincard2)
            || Array.IndexOf(Xueqinjincard2.RandomAffinityCardTypes, cardType) >= 0)
            return BondEarStyle.Affinity;

        if (cardType == typeof(Xueqinjincard1)
            || Array.IndexOf(Xueqinjincard1.RandomEstrangementCardTypes, cardType) >= 0)
            return BondEarStyle.Estrangement;

        return BondEarStyle.None;
    }

    private static bool ShouldUseActiveDogEarMaterial(CardModel card, BondEarStyle style)
    {
        return style switch
        {
            BondEarStyle.Affinity or BondEarStyle.Estrangement => CanTriggerBondBonus(card, style),
            BondEarStyle.BadEnding or BondEarStyle.TrueEnding
                or BondEarStyle.YalisaQinjin or BondEarStyle.PinkGray => true,
            _ => false
        };
    }

    private static BondFrameGlowStyle GetFrameGlowStyle(CardModel card, BondEarStyle style)
    {
        return style switch
        {
            BondEarStyle.Affinity => CanTriggerBondBonus(card, style)
                ? BondFrameGlowStyle.Affinity
                : BondFrameGlowStyle.None,
            BondEarStyle.Estrangement => CanTriggerBondBonus(card, style)
                ? BondFrameGlowStyle.Estrangement
                : BondFrameGlowStyle.None,
            BondEarStyle.PinkGray => GetCurrentBondFrameGlowStyle(card),
            _ => BondFrameGlowStyle.None
        };
    }

    private static BondFrameGlowStyle GetCurrentBondFrameGlowStyle(CardModel card)
    {
        if (!card.IsMutable)
            return BondFrameGlowStyle.None;

        var owner = card.Owner;
        if (owner?.Creature is not { } creature)
            return BondFrameGlowStyle.None;

        var bond = creature.GetPower<BondPower>();
        if (bond is null)
            return BondFrameGlowStyle.None;

        if (bond.Affinity > bond.Estrangement)
            return BondFrameGlowStyle.Affinity;

        if (bond.Estrangement > bond.Affinity)
            return BondFrameGlowStyle.Estrangement;

        return BondFrameGlowStyle.None;
    }

    private static bool CanTriggerBondBonus(CardModel card, BondEarStyle style)
    {
        if (!card.IsMutable)
            return false;

        var owner = card.Owner;
        if (owner?.Creature is not { } creature)
            return false;

        var bond = creature.GetPower<BondPower>();
        return bond is not null && (style switch
        {
            BondEarStyle.Affinity => bond.Affinity + 1 > bond.Estrangement,
            BondEarStyle.Estrangement => bond.Estrangement + 1 > bond.Affinity,
            _ => false
        });
    }

    private static ShaderMaterial GetDogEarMaterial(BondEarStyle style, bool active)
    {
        return style switch
        {
            BondEarStyle.BadEnding => BadEndingDogEarMaterial.Value,
            BondEarStyle.TrueEnding => TrueEndingDogEarMaterial.Value,
            BondEarStyle.YalisaQinjin => YalisaQinjinDogEarMaterial.Value,
            BondEarStyle.PinkGray => PinkGrayDogEarMaterial.Value,
            BondEarStyle.Estrangement => active
                ? ActiveEstrangementCatEarMaterial.Value
                : NormalEstrangementCatEarMaterial.Value,
            _ => active
                ? ActiveAffinityDogEarMaterial.Value
                : NormalAffinityDogEarMaterial.Value
        };
    }

    private static ShaderMaterial GetFrameGlowMaterial(BondFrameGlowStyle style)
    {
        return style == BondFrameGlowStyle.Estrangement
            ? EstrangementFrameGlowMaterial.Value
            : AffinityFrameGlowMaterial.Value;
    }

    private static ShaderMaterial CreateDogEarMaterial(
        bool active,
        Color accentColor,
        Color shadowColor,
        Color highlightColor,
        float glowIntensity = 1f)
    {
        return CreateSplitDogEarMaterial(
            active,
            accentColor,
            shadowColor,
            highlightColor,
            accentColor,
            shadowColor,
            highlightColor,
            glowIntensity);
    }

    private static ShaderMaterial CreateSplitDogEarMaterial(
        bool active,
        Color leftAccentColor,
        Color leftShadowColor,
        Color leftHighlightColor,
        Color rightAccentColor,
        Color rightShadowColor,
        Color rightHighlightColor,
        float glowIntensity = 1f)
    {
        var material = new ShaderMaterial
        {
            Shader = DogEarShader.Value,
        };
        material.SetShaderParameter("left_accent_color", leftAccentColor);
        material.SetShaderParameter("left_shadow_color", leftShadowColor);
        material.SetShaderParameter("left_highlight_color", leftHighlightColor);
        material.SetShaderParameter("right_accent_color", rightAccentColor);
        material.SetShaderParameter("right_shadow_color", rightShadowColor);
        material.SetShaderParameter("right_highlight_color", rightHighlightColor);
        material.SetShaderParameter("glow_enabled", active ? 1f : 0f);
        material.SetShaderParameter("glow_intensity", glowIntensity);
        return material;
    }

    private static Shader CreateDogEarShader()
    {
        return new Shader
        {
            Code = """
shader_type canvas_item;
render_mode blend_mix;

uniform vec4 left_accent_color : source_color = vec4(1.0, 0.58, 0.82, 1.0);
uniform vec4 left_shadow_color : source_color = vec4(0.20, 0.05, 0.13, 1.0);
uniform vec4 left_highlight_color : source_color = vec4(1.0, 0.95, 1.0, 1.0);
uniform vec4 right_accent_color : source_color = vec4(1.0, 0.58, 0.82, 1.0);
uniform vec4 right_shadow_color : source_color = vec4(0.20, 0.05, 0.13, 1.0);
uniform vec4 right_highlight_color : source_color = vec4(1.0, 0.95, 1.0, 1.0);
uniform float pulse_speed : hint_range(0.0, 5.0, 0.05) = 1.1;
uniform float glow_enabled : hint_range(0.0, 1.0, 1.0) = 0.0;
uniform float glow_intensity : hint_range(1.0, 2.0, 0.05) = 1.0;

float ear_shape(vec2 uv, float center_x, float tip_y, float base_y, float width, float slant) {
    float vertical = smoothstep(tip_y - 0.02, tip_y + 0.025, uv.y) *
        (1.0 - smoothstep(base_y, base_y + 0.025, uv.y));
    float t = clamp((uv.y - tip_y) / max(base_y - tip_y, 0.001), 0.0, 1.0);
    float half_width = mix(0.008, width, pow(t, 0.78));
    float shifted_center = center_x + slant * (1.0 - t);
    float horizontal = 1.0 - smoothstep(half_width, half_width + 0.018, abs(uv.x - shifted_center));
    return vertical * horizontal;
}

void fragment() {
    vec4 inherited_color = COLOR;
    float pulse = 0.5 + 0.5 * sin(TIME * pulse_speed);

    float left_outer = ear_shape(UV, 0.31, 0.10, 0.86, 0.122, -0.070);
    float right_outer = ear_shape(UV, 0.69, 0.10, 0.86, 0.122, 0.070);
    float left_inner = ear_shape(UV, 0.31, 0.35, 0.78, 0.058, -0.035);
    float right_inner = ear_shape(UV, 0.69, 0.35, 0.78, 0.058, 0.035);

    float left_outline = clamp(left_outer - left_inner * 0.88, 0.0, 1.0);
    float right_outline = clamp(right_outer - right_inner * 0.88, 0.0, 1.0);
    float outline = max(left_outline, right_outline);
    float inner_tint = max(left_inner, right_inner);
    float center_gap = smoothstep(0.055, 0.080, abs(UV.x - 0.5));

    float active_alpha = mix(0.58, 0.84, glow_enabled);
    float alpha = (outline * active_alpha + inner_tint * 0.14 + glow_enabled * max(left_outer, right_outer) * 0.28) * center_gap;
    alpha *= 0.86 + pulse * 0.14;

    float vertical_gradient = clamp((UV.y - 0.10) / 0.76, 0.0, 1.0);
    float shimmer = 0.5 + 0.5 * sin(UV.y * 7.0 + TIME * 1.2);
    float gradient = clamp(0.12 + vertical_gradient * 0.62 + shimmer * 0.08, 0.0, 0.82);
    vec3 left_normal_color = mix(left_shadow_color.rgb, left_accent_color.rgb, 0.72 + pulse * 0.18);
    vec3 right_normal_color = mix(right_shadow_color.rgb, right_accent_color.rgb, 0.72 + pulse * 0.18);
    vec3 left_glow_color = mix(left_accent_color.rgb, left_highlight_color.rgb, gradient);
    vec3 right_glow_color = mix(right_accent_color.rgb, right_highlight_color.rgb, gradient);
    vec3 left_color = mix(left_normal_color, left_glow_color, glow_enabled);
    vec3 right_color = mix(right_normal_color, right_glow_color, glow_enabled);
    float left_weight = max(left_outer, left_inner);
    float right_weight = max(right_outer, right_inner);
    float side_mix = right_weight / max(left_weight + right_weight, 0.001);
    vec3 color = mix(left_color, right_color, side_mix);
    color *= mix(1.0, glow_intensity, glow_enabled);
    COLOR = vec4(color * inherited_color.rgb, clamp(alpha, 0.0, mix(0.74, 0.94, glow_enabled)) * inherited_color.a);
}
""",
        };
    }

    private static ShaderMaterial CreateFrameGlowMaterial(Color primaryColor, Color secondaryColor)
    {
        var material = new ShaderMaterial
        {
            Shader = FrameGlowShader.Value
        };
        material.SetShaderParameter("primary_color", primaryColor);
        material.SetShaderParameter("secondary_color", secondaryColor);
        return material;
    }

    private static Shader CreateFrameGlowShader()
    {
        return new Shader
        {
            Code = """
shader_type canvas_item;
render_mode blend_add;

uniform vec4 primary_color : source_color = vec4(1.0, 0.50, 0.78, 1.0);
uniform vec4 secondary_color : source_color = vec4(1.0, 0.96, 1.0, 1.0);

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
    float alpha = sharp_edge * 0.56 + soft_glow * corner_weight;

    float flow = fract(UV.x * 0.70 + UV.y * 0.32 + TIME * 0.22);
    vec3 color = mix(primary_color.rgb, secondary_color.rgb, smoothstep(0.20, 0.92, flow));
    COLOR = vec4(color * inherited_color.rgb, clamp(alpha, 0.0, 0.78) * inherited_color.a);
}
""",
        };
    }
}
