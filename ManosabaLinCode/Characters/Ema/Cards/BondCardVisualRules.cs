using Godot;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Cards;

internal enum BondCardVisualStyle
{
    None,
    Affinity,
    Estrangement,
    BadEnding,
    TrueEnding,
    YalisaQinjin,
    PinkGray
}

internal enum BondCardFrameGlowStyle
{
    None,
    Affinity,
    Estrangement
}

internal static class BondCardVisualRules
{
    private static readonly Color AffinityColor = new(1f, 0.50f, 0.78f);
    private static readonly Color AffinityHighlightColor = new(1f, 0.96f, 1f);
    private static readonly Color EstrangementColor = new(0.70f, 0f, 0.04f);
    private static readonly Color EstrangementShadowColor = new(0.16f, 0f, 0.01f);
    private static readonly Color YalisaQinjinColor = new(1f, 0.42f, 0.72f);
    private static readonly Color YalisaQinjinEdgeColor = new(1f, 0.08f, 0.10f);

    internal static BondCardVisualStyle GetStyle(CardModel card)
    {
        var cardType = card.GetType();
        if (cardType == typeof(EmaBadEnding))
            return BondCardVisualStyle.BadEnding;

        if (cardType == typeof(EmaTrueEnding))
            return BondCardVisualStyle.TrueEnding;

        if (cardType == typeof(Yalisaqinjin))
            return BondCardVisualStyle.YalisaQinjin;

        if (cardType == typeof(BondExchangecard) ||
            cardType == typeof(Xueshuyuancard))
            return BondCardVisualStyle.PinkGray;

        if (cardType == typeof(Xueqinjincard2) ||
            Array.IndexOf(Xueqinjincard2.RandomAffinityCardTypes, cardType) >= 0)
            return BondCardVisualStyle.Affinity;

        if (cardType == typeof(Xueqinjincard1) ||
            Array.IndexOf(Xueqinjincard1.RandomEstrangementCardTypes, cardType) >= 0)
            return BondCardVisualStyle.Estrangement;

        return BondCardVisualStyle.None;
    }

    internal static bool ShouldUseActiveDogEarMaterial(CardModel card, BondCardVisualStyle style)
    {
        return style switch
        {
            BondCardVisualStyle.Affinity or BondCardVisualStyle.Estrangement => CanTriggerBondBonus(card, style),
            BondCardVisualStyle.BadEnding or BondCardVisualStyle.TrueEnding
                or BondCardVisualStyle.YalisaQinjin or BondCardVisualStyle.PinkGray => true,
            _ => false
        };
    }

    internal static BondCardFrameGlowStyle GetFrameGlowStyle(CardModel card, BondCardVisualStyle style)
    {
        return style switch
        {
            BondCardVisualStyle.Affinity => CanTriggerBondBonus(card, style)
                ? BondCardFrameGlowStyle.Affinity
                : BondCardFrameGlowStyle.None,
            BondCardVisualStyle.Estrangement => CanTriggerBondBonus(card, style)
                ? BondCardFrameGlowStyle.Estrangement
                : BondCardFrameGlowStyle.None,
            BondCardVisualStyle.PinkGray => GetCurrentBondFrameGlowStyle(card),
            _ => BondCardFrameGlowStyle.None
        };
    }

    internal static Color? GetHandOutlineColor(CardModel card)
    {
        if (!card.CanPlay())
            return null;

        var cardType = card.GetType();
        if (cardType == typeof(Emamqinjin))
            return AnimatedGradient(AffinityColor, AffinityHighlightColor);
        if (cardType == typeof(Emamshuyuan))
            return AnimatedGradient(EstrangementColor, EstrangementShadowColor);

        var style = GetStyle(card);
        return style switch
        {
            BondCardVisualStyle.Affinity => CanTriggerBondBonus(card, style)
                ? AnimatedGradient(AffinityColor, AffinityHighlightColor)
                : null,
            BondCardVisualStyle.Estrangement => CanTriggerBondBonus(card, style)
                ? AnimatedGradient(EstrangementColor, EstrangementShadowColor)
                : null,
            BondCardVisualStyle.PinkGray => GetCurrentBondFrameGlowStyle(card) switch
            {
                BondCardFrameGlowStyle.Affinity => AnimatedGradient(AffinityColor, AffinityHighlightColor),
                BondCardFrameGlowStyle.Estrangement => AnimatedGradient(EstrangementColor, EstrangementShadowColor),
                _ => null
            },
            BondCardVisualStyle.YalisaQinjin => AnimatedGradient(YalisaQinjinColor, YalisaQinjinEdgeColor),
            BondCardVisualStyle.BadEnding => AnimatedGradient(EstrangementColor, AffinityColor),
            BondCardVisualStyle.TrueEnding => AnimatedGradient(AffinityColor, EstrangementColor),
            _ => null
        };
    }

    private static BondCardFrameGlowStyle GetCurrentBondFrameGlowStyle(CardModel card)
    {
        if (!card.IsMutable)
            return BondCardFrameGlowStyle.None;

        var owner = card.Owner;
        if (owner?.Creature is not { } creature)
            return BondCardFrameGlowStyle.None;

        var bond = creature.GetPower<BondPower>();
        if (bond is null)
            return BondCardFrameGlowStyle.None;

        if (bond.Affinity > bond.Estrangement)
            return BondCardFrameGlowStyle.Affinity;

        if (bond.Estrangement > bond.Affinity)
            return BondCardFrameGlowStyle.Estrangement;

        return BondCardFrameGlowStyle.None;
    }

    private static bool CanTriggerBondBonus(CardModel card, BondCardVisualStyle style)
    {
        if (!card.IsMutable)
            return false;

        var owner = card.Owner;
        if (owner?.Creature is not { } creature)
            return false;

        var bond = creature.GetPower<BondPower>();
        return bond is not null && (style switch
        {
            BondCardVisualStyle.Affinity => bond.Affinity + 1 > bond.Estrangement,
            BondCardVisualStyle.Estrangement => bond.Estrangement + 1 > bond.Affinity,
            _ => false
        });
    }

    private static Color AnimatedGradient(Color first, Color second)
    {
        var seconds = Time.GetTicksMsec() / 1000.0;
        var t = 0.5f + 0.5f * Mathf.Sin((float)seconds * 1.6f);
        t = t * t * (3f - 2f * t);
        return new Color(
            Mathf.Lerp(first.R, second.R, t),
            Mathf.Lerp(first.G, second.G, t),
            Mathf.Lerp(first.B, second.B, t),
            1f);
    }
}
