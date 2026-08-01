using HarmonyLib;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace ManosabaLin.Characters.Yalisalin.Components;

internal static class YalisalinFireComponentSelectionRegistry
{
    private static readonly Stack<YalisalinFireComponentContext> Contexts = [];

    public static YalisalinFireComponentContext? Current => Contexts.Count == 0 ? null : Contexts.Peek();

    public static IDisposable Begin(YalisalinFireComponentContext context)
    {
        Contexts.Push(context);
        return new Scope(context);
    }

    internal static bool TryHandleRightClick(NCardGridSelectionScreen screen, CardModel card)
    {
        var context = Current;
        if (context == null)
            return false;

        if (!context.ChoiceOptions.Contains(card))
            return false;

        if (!context.TryApplyNextRightClick(card))
            return false;

        UpdatePrompt(screen, context, card);
        return true;
    }

    internal static void TryUpdatePromptForHover(NCardHolder holder, bool isHovered)
    {
        var context = Current;
        var card = holder.CardModel;
        if (context == null || card == null || !context.ChoiceOptions.Contains(card))
            return;

        var screen = FindSelectionScreen(holder);
        if (screen == null)
            return;

        UpdatePrompt(screen, context, isHovered ? card : null);
    }

    internal static void UpdatePrompt(
        NCardGridSelectionScreen screen,
        YalisalinFireComponentContext context,
        CardModel? hoveredCard = null)
    {
        var label = screen.GetNodeOrNull<MegaRichTextLabel>("%BottomLabel");
        if (label != null)
            label.Text = context.SelectionPromptTextFor(hoveredCard);
    }

    private static NCardGridSelectionScreen? FindSelectionScreen(Node node)
    {
        for (var current = node; current != null; current = current.GetParent())
        {
            if (current is NCardGridSelectionScreen screen)
                return screen;
        }

        return null;
    }

    private sealed class Scope(YalisalinFireComponentContext context) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (Contexts.Count > 0 && ReferenceEquals(Contexts.Peek(), context))
            {
                Contexts.Pop();
                return;
            }

            var remaining = Contexts.Where(item => !ReferenceEquals(item, context)).Reverse().ToArray();
            Contexts.Clear();
            foreach (var item in remaining)
                Contexts.Push(item);
        }
    }
}

[HarmonyPatch(typeof(NCardGridSelectionScreen), "ShowCardDetail")]
internal static class YalisalinFireComponentSelectionRightClickPatch
{
    private static bool Prefix(NCardGridSelectionScreen __instance, CardModel card)
    {
        return !YalisalinFireComponentSelectionRegistry.TryHandleRightClick(__instance, card);
    }
}

[HarmonyPatch(typeof(NCardHolder), "OnFocus")]
internal static class YalisalinFireComponentSelectionHolderFocusPatch
{
    private static void Postfix(NCardHolder __instance)
    {
        YalisalinFireComponentSelectionRegistry.TryUpdatePromptForHover(__instance, isHovered: true);
    }
}

[HarmonyPatch(typeof(NCardHolder), "OnUnfocus")]
internal static class YalisalinFireComponentSelectionHolderUnfocusPatch
{
    private static void Postfix(NCardHolder __instance)
    {
        YalisalinFireComponentSelectionRegistry.TryUpdatePromptForHover(__instance, isHovered: false);
    }
}
