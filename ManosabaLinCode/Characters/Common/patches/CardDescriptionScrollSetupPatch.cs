using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2RitsuLib.Patching.Models;

namespace  ManosabaLin.Scripts.Patches;


/// <summary>
/// 启用卡牌描述内置滚动条，并避免描述文本控件抢走卡牌 hover。
/// </summary>
public sealed class CardDescriptionScrollSetupPatch : IPatchMethod
{
    public static string PatchId => "your_mod_card_description_scroll_setup";

    public static string Description => "Enable scrolling for overflowing card descriptions";

    public static bool IsCritical => false;

    private const string ScrollInstalledMeta = "YourModDescriptionScrollInstalled";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NCard), "_Ready")];
    }

    public static void Postfix(NCard __instance)
    {
        MegaRichTextLabel? descriptionLabel = __instance.GetNodeOrNull<MegaRichTextLabel>("%DescriptionLabel");
        if (descriptionLabel is null || descriptionLabel.HasMeta(ScrollInstalledMeta))
        {
            return;
        }

        descriptionLabel.ScrollActive = true;
        descriptionLabel.ScrollFollowing = false;

        // 不要让描述文本控件抢卡牌命中，否则战斗中 hover/选中会闪烁。
        descriptionLabel.MouseFilter = Control.MouseFilterEnum.Ignore;

        descriptionLabel.SetMeta(ScrollInstalledMeta, true);
    }
}

/// <summary>
/// 鼠标在卡牌命中框上时，用滚轮滚动该卡牌描述。
/// </summary>
public sealed class CardDescriptionHitboxScrollPatch : IPatchMethod
{
    public static string PatchId => "your_mod_card_description_hitbox_scroll";

    public static string Description => "Scroll card descriptions from the card hitbox";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NClickableControl), "_GuiInput")];
    }

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

/// <summary>
/// 已选中卡牌、进入鼠标目标选择阶段后，无论鼠标停在哪里都可滚动中央卡牌描述。
/// </summary>
public sealed class SelectedCardDescriptionScrollPatch : IPatchMethod
{
    public static string PatchId => "your_mod_selected_card_description_scroll";

    public static string Description => "Scroll the selected card description during mouse targeting";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMouseCardPlay), "_Input")];
    }

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

    public static bool TryScroll(MegaRichTextLabel descriptionLabel, double direction)
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
