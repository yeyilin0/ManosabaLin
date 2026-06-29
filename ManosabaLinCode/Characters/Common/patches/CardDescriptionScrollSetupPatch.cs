using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NCard), nameof(NCard._Ready))]
public static class CardDescriptionScrollSetupPatch
{
    private const string ScrollInstalledMeta = "ManosabaLinDescriptionScrollInstalled";

    public static void Postfix(NCard __instance)
    {
        MegaRichTextLabel? descriptionLabel = __instance.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");
        if (descriptionLabel is null || descriptionLabel.HasMeta(ScrollInstalledMeta))
        {
            return;
        }

        descriptionLabel.ScrollActive = true;
        descriptionLabel.ScrollFollowing = false;
        descriptionLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        descriptionLabel.SetMeta(ScrollInstalledMeta, true);
    }
}

[HarmonyPatch(typeof(NClickableControl), nameof(NClickableControl._GuiInput))]
public static class CardDescriptionHitboxScrollPatch
{
    public static void Postfix(NClickableControl __instance, InputEvent inputEvent)
    {
        if (!CardDescriptionScrollHelper.TryGetScrollDirection(inputEvent, out double direction))
        {
            return;
        }

        NCardHolder? cardHolder = FindCardHolder(__instance);
        NCard? cardNode = cardHolder?.CardNode;
        if (cardNode is null)
        {
            return;
        }

        if (CardDescriptionScrollHelper.TryScroll(cardNode, direction))
        {
            __instance.AcceptEvent();
        }
    }

    private static NCardHolder? FindCardHolder(Node node)
    {
        for (Node? current = node; current is not null; current = current.GetParent())
        {
            if (current is NCardHolder cardHolder)
            {
                return cardHolder;
            }
        }

        return null;
    }
}

[HarmonyPatch(typeof(NMouseCardPlay), nameof(NMouseCardPlay._Input))]
public static class SelectedCardDescriptionScrollPatch
{
    public static bool Prefix(NMouseCardPlay __instance, InputEvent inputEvent)
    {
        if (!CardDescriptionScrollHelper.TryGetScrollDirection(inputEvent, out double direction))
        {
            return true;
        }

        if (__instance.Holder?.CardNode is not { } cardNode)
        {
            return true;
        }

        if (!CardDescriptionScrollHelper.TryScroll(cardNode, direction))
        {
            return true;
        }

        __instance.GetViewport()?.SetInputAsHandled();
        return false;
    }
}

internal static class CardDescriptionScrollHelper
{
    private const double MinScrollStep = 3.0;
    private const double PageScrollRatio = 0.25;

    public static bool TryGetScrollDirection(InputEvent inputEvent, out double direction)
    {
        direction = inputEvent switch
        {
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp } => -1.0,
            InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelDown } => 1.0,
            _ => 0.0
        };

        return direction != 0.0;
    }

    public static bool TryScroll(NCard cardNode, double direction)
    {
        MegaRichTextLabel? descriptionLabel = cardNode.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");
        return descriptionLabel is not null && TryScroll(descriptionLabel, direction);
    }

    private static bool TryScroll(MegaRichTextLabel descriptionLabel, double direction)
    {
        VScrollBar scrollBar = descriptionLabel.GetVScrollBar();
        double maxValue = Math.Max(scrollBar.MinValue, scrollBar.MaxValue - scrollBar.Page);
        if (maxValue <= scrollBar.MinValue)
        {
            return false;
        }

        double scrollStep = Math.Max(MinScrollStep, scrollBar.Page * PageScrollRatio);
        scrollBar.Value = Math.Clamp(
            scrollBar.Value + direction * scrollStep,
            scrollBar.MinValue,
            maxValue);

        return true;
    }
}
