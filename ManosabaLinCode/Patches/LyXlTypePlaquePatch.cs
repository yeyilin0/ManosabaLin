using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Sherrylin.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards;
using System.Reflection;
using HiroError = ManosabaLin.Characters.Hiro.Cards.Error;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NCard))]
internal static class LyXlTypePlaquePatch
{
    private const string ReplacementMeta = "manosabalin_lyxl_type_plaque_replacement";

    private static readonly FieldInfo? TypePlaqueField = AccessTools.Field(typeof(NCard), "_typePlaque");
    private static readonly FieldInfo? TypeLabelField = AccessTools.Field(typeof(NCard), "_typeLabel");
    private static readonly FieldInfo? AncientTextBgField = AccessTools.Field(typeof(NCard), "_ancientTextBg");

    [HarmonyPatch("UpdateTypePlaque")]
    [HarmonyPrefix]
    private static bool UpdateTypePlaquePrefix(NCard __instance)
    {
        if (!ShouldRemoveTypePlaque(__instance))
        {
            return SetTypePlaqueVisible(__instance, true);
        }

        RemoveTypePlaqueForLyXl(__instance);
        return false;
    }

    [HarmonyPatch("UpdateTypePlaque")]
    [HarmonyFinalizer]
    private static Exception? UpdateTypePlaqueFinalizer(NCard __instance, Exception? __exception)
    {
        return SuppressDisposedTypePlaqueException(__instance, __exception);
    }

    [HarmonyPatch("UpdateTypePlaqueSizeAndPosition")]
    [HarmonyPrefix]
    private static bool UpdateTypePlaqueSizeAndPositionPrefix(NCard __instance)
    {
        if (!ShouldRemoveTypePlaque(__instance))
        {
            return SetTypePlaqueVisible(__instance, true);
        }

        RemoveTypePlaqueForLyXl(__instance);
        return false;
    }

    [HarmonyPatch("UpdateTypePlaqueSizeAndPosition")]
    [HarmonyFinalizer]
    private static Exception? UpdateTypePlaqueSizeAndPositionFinalizer(NCard __instance, Exception? __exception)
    {
        return SuppressDisposedTypePlaqueException(__instance, __exception);
    }

    [HarmonyPatch("Reload")]
    [HarmonyPrefix]
    private static void ReloadPrefix(NCard __instance)
    {
        if (!ShouldRemoveTypePlaque(__instance))
            SetTypePlaqueVisible(__instance, true);
    }

    [HarmonyPatch("Reload")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void ReloadPostfix(NCard __instance)
    {
        if (ShouldRemoveTypePlaque(__instance))
            RemoveTypePlaqueForLyXl(__instance);

        HideAncientTextBgIfNeeded(__instance);
    }

    [HarmonyPatch(nameof(NCard.UpdateVisuals), [typeof(PileType), typeof(CardPreviewMode)])]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void UpdateVisualsPostfix(NCard __instance)
    {
        HideAncientTextBgIfNeeded(__instance);
    }

    private static bool ShouldRemoveTypePlaque(NCard card)
    {
        return GodotObject.IsInstanceValid(card) && card.Model is
            LyXl or
            HiroBadEnding or
            AnanlinFinishedDraft or
            AnanlinBadEnding or
            AnanlinCocoMultiverseMagic or
            HiroError or
            Yalisaqinjin or
            Hirodeath or
            Emadeath or
            EmaForgottenOne or
            Sherrydeath or
            Sherrybadending or
            Anandeath or
            EmaBadEnding or
            TheEnd or
            AnanlinBombDisposalExpert or
            EmaTrueEnding or
            MeruruAndEma or
            Lamort or
            AnanlinLover or
            Justice or
            TheFool or
            SamePlaceTruth or
            SamePlacePendingTruth;
    }

    private static bool ShouldHideAncientTextBg(NCard card)
    {
        return GodotObject.IsInstanceValid(card) && card.Model is
            MeruruAndEma or
            SamePlaceTruth or
            SamePlacePendingTruth;
    }

    private static void RemoveTypePlaqueForLyXl(NCard card)
    {
        var typePlaque = GetNode(card, TypePlaqueField, "%TypePlaque");
        var typeLabel = GetNode(card, TypeLabelField, "%TypeLabel");

        if (IsReplacement(typePlaque) || IsReplacement(typeLabel))
        {
            SetNodeVisible(typePlaque, false);
            SetNodeVisible(typeLabel, false);
            return;
        }

        if (IsNodeValid(typePlaque) &&
            IsNodeValid(typeLabel) &&
            IsAncestorOf(typePlaque!, typeLabel!))
        {
            var labelPath = typePlaque!.GetPathTo(typeLabel);
            if (TryReplaceNode(card, TypePlaqueField, typePlaque, out var replacementPlaque))
            {
                var replacementLabel = replacementPlaque.GetNodeOrNull<Node>(labelPath);
                if (IsNodeValid(replacementLabel))
                {
                    MarkReplacement(replacementLabel!);
                    TypeLabelField?.SetValue(card, replacementLabel);
                    SetNodeVisible(replacementLabel, false);
                }

                QueueFreeNode(typePlaque);
                return;
            }
        }

        if (TryReplaceNode(card, TypePlaqueField, typePlaque, out var replacementTypePlaque))
            QueueFreeNode(typePlaque);

        if (TryReplaceNode(card, TypeLabelField, typeLabel, out var replacementTypeLabel))
            QueueFreeNode(typeLabel);

        SetNodeVisible(replacementTypePlaque, false);
        SetNodeVisible(replacementTypeLabel, false);
    }

    private static Node? GetNode(NCard card, FieldInfo? field, NodePath fallbackPath)
    {
        if (field?.GetValue(card) is Node fieldNode && IsNodeValid(fieldNode))
            return fieldNode;

        var fallback = GetFallbackNode(card, fallbackPath);
        if (!IsNodeValid(fallback))
            return null;

        TrySetField(card, field, fallback);
        return fallback;
    }

    private static Node? GetFallbackNode(NCard card, NodePath fallbackPath)
    {
        try
        {
            var fallback = card.GetNodeOrNull<Node>(fallbackPath);
            if (IsNodeValid(fallback))
                return fallback;
        }
        catch (ObjectDisposedException)
        {
        }

        var name = fallbackPath.ToString().TrimStart('%');
        return string.IsNullOrEmpty(name) ? null : FindChildByName(card, name);
    }

    private static Node? FindChildByName(Node root, string name)
    {
        foreach (var child in root.GetChildren())
        {
            if (!IsNodeValid(child))
                continue;

            if (child.Name == name)
                return child;

            var nested = FindChildByName(child, name);
            if (IsNodeValid(nested))
                return nested;
        }

        return null;
    }

    private static bool TryReplaceNode(NCard card, FieldInfo? field, Node? original, out Node? replacement)
    {
        replacement = null;
        if (field == null || !IsNodeValid(original))
            return false;

        var parent = original!.GetParent();
        if (!IsNodeValid(parent))
            return false;

        replacement = original.Duplicate();
        if (!IsNodeValid(replacement))
            return false;

        var originalName = original.Name;
        var originalIndex = original.GetIndex();
        var originalOwner = original.Owner;
        var originalUniqueNameInOwner = original.UniqueNameInOwner;

        original.UniqueNameInOwner = false;
        original.Name = $"{originalName}_ManosabaLinQueuedFree";

        replacement!.Name = originalName;
        MarkReplacement(replacement);
        SetNodeVisible(replacement, false);

        parent!.AddChild(replacement);
        parent.MoveChild(replacement, originalIndex);
        replacement.Owner = originalOwner;
        replacement.UniqueNameInOwner = originalUniqueNameInOwner;
        TrySetField(card, field, replacement);
        return true;
    }

    private static bool IsAncestorOf(Node ancestor, Node descendant)
    {
        for (var parent = descendant.GetParent(); parent != null; parent = parent.GetParent())
        {
            if (parent == ancestor)
                return true;
        }

        return false;
    }

    private static bool SetTypePlaqueVisible(NCard card, bool visible)
    {
        var typePlaque = GetNode(card, TypePlaqueField, "%TypePlaque");
        var typeLabel = GetNode(card, TypeLabelField, "%TypeLabel");

        if (!IsNodeValid(typePlaque) || !IsNodeValid(typeLabel))
            return false;

        SetNodeVisible(typePlaque, visible);
        SetNodeVisible(typeLabel, visible);
        return true;
    }

    private static void HideAncientTextBgIfNeeded(NCard card)
    {
        if (!ShouldHideAncientTextBg(card))
            return;

        var ancientTextBg = GetNode(card, AncientTextBgField, "%AncientTextBg");
        SetNodeVisible(ancientTextBg, false);
    }

    private static void SetNodeVisible(Node? node, bool visible)
    {
        if (!IsNodeValid(node))
            return;

        if (node is CanvasItem canvasItem)
            canvasItem.Visible = visible;
    }

    private static void MarkReplacement(Node node)
    {
        node.SetMeta(ReplacementMeta, true);
    }

    private static bool IsReplacement(Node? node)
    {
        return IsNodeValid(node) && node!.HasMeta(ReplacementMeta);
    }

    private static Exception? SuppressDisposedTypePlaqueException(NCard card, Exception? exception)
    {
        if (exception is not ObjectDisposedException)
            return exception;

        SetTypePlaqueVisible(card, false);
        return null;
    }

    private static void TrySetField(NCard card, FieldInfo? field, Node? node)
    {
        if (field == null || !IsNodeValid(node))
            return;

        try
        {
            if (field.FieldType.IsInstanceOfType(node))
                field.SetValue(card, node);
        }
        catch (ArgumentException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static void QueueFreeNode(Node? node)
    {
        if (!IsNodeValid(node))
            return;

        node!.QueueFree();
    }

    private static bool IsNodeValid(Node? node)
    {
        try
        {
            return node != null &&
                   GodotObject.IsInstanceValid(node) &&
                   !node.IsQueuedForDeletion();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }
}
