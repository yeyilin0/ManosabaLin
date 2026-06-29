using System;
using System.Collections.Generic;
using FusionMod.Core;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace FusionMod.Patches;

[HarmonyPatch(typeof(NCreature), "_Ready")]
public static class FusionVisualPatch
{
    static readonly List<NCreature> _nodes = new();
    static bool _subscribed;

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        if (!FusionManager.IsFusionModeActive) return;
        if (!FusionManager.IsCurrentNodeFusion()) return;
        try { var e = __instance.Entity; if (e == null || e.IsPlayer) return; _nodes.Add(__instance);
            if (!_subscribed) { _subscribed = true; FusionMonsterSetupPatch.OnPairsBuilt += CreateStands; } } catch { }
    }

    static readonly string[] IdleTry = {"idle_loop","Idle_Loop","idle","Idle","idle1","stand","animation"};

    static void CreateStands()
    {
        _subscribed = false;
        try
        {
            var c2n = new Dictionary<Creature, NCreature>();
            foreach (var nc in _nodes) { if (GodotObject.IsInstanceValid(nc) && nc.Entity != null && !nc.Entity.IsPlayer) c2n[nc.Entity] = nc; }

            foreach (var kv in FusionMonsterSetupPatch.PartnerVisualPaths)
            {
                var monster = kv.Key; var path = kv.Value;
                var mc = monster.Creature; if (mc == null || !c2n.TryGetValue(mc, out var myNode)) continue;

                var scene = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Reuse);
                if (scene == null) continue;
                var ghost = scene.Instantiate<NCreatureVisuals>(PackedScene.GenEditState.Disabled);
                if (ghost == null) continue;

                ghost.Name = "FusionStand";
                ghost.ShowBehindParent = true;
                ghost.Modulate = new Color(0.4f, 0.5f, 1.0f, 0.45f);
                ghost.Scale = new Vector2(1.05f, 1.05f);
                ghost.Position = new Vector2(80f, -60f);
                ghost.SetProcess(true);
                ghost.SetPhysicsProcess(true);
                myNode.AddChild(ghost);

                if (ghost.SpineBody != null)
                    foreach (var n in IdleTry) if (ghost.SpineBody.HasAnimation(n)) { new SpineAnimationAccess(ghost.SpineBody).AddAnimation(n, 0f, true, 0); break; }
            }
        }
        catch (Exception ex) { FusionModMain.Logger?.Warn($"替身: {ex.Message}", 3); }
        finally { _nodes.Clear(); }
    }
}

/// <summary>动画同步 + 调试日志</summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.CreatureCmd), "TriggerAnim",
    typeof(Creature), typeof(string), typeof(float))]
public static class FusionGhostAnimSync
{
    static readonly string[] IdleTry = {"idle_loop","Idle_Loop","idle","Idle"};
    static readonly string[] AtkTry = {"attack","Attack","attack1","attack_1","AttackTrigger","swing","swing_1","swing_2","beat","slash","claw","bite","smash","punch","shoot","stab"};
    static readonly string[] CastTry = {"cast","Cast","spell","buff","roar","sharpen","powerup","PowerUp","channel","summon","taunt","howl","scream","shout","battlecry"};
    static readonly string[] HitTry = {"hit","Hit","hurt","Hurt","damage","hit1","damaged"};
    static readonly string[] DieTry = {"die","Die","death","Death","dead","Dead"};

    [HarmonyPostfix]
    public static void Postfix(Creature creature, string triggerName)
    {
        if (creature.IsPlayer) return;
        try
        {
            var node = creature.GetCreatureNode();
            var ghost = node?.GetNodeOrNull<NCreatureVisuals>("FusionStand");
            if (ghost?.SpineBody == null) return;

            var spine = ghost.SpineBody;
            var sa = new SpineAnimationAccess(spine);

            // 选候选列表
            var tl = triggerName.ToLowerInvariant();
            string[][] candidates = tl switch
            {
                "attack" or "attacktrigger" => new[]{AtkTry},
                "cast" or "powerup" => new[]{CastTry},
                "hit" => new[]{HitTry},
                "dead" or "die" => new[]{DieTry},
                _ => null
            };

            if (candidates == null) return;

            string? found = null;
            foreach (var g in candidates)
                foreach (var n in g)
                    if (spine.HasAnimation(n)) { found = n; break; }

            if (found != null)
            {
                FusionModMain.Logger?.Info($"替身动画: {found} (触发={triggerName})", 3);
                sa.SetAnimation(found, false, 0);
                foreach (var n in IdleTry) if (spine.HasAnimation(n)) { sa.AddAnimation(n, 0f, true, 0); break; }
            }
            else
            {
                FusionModMain.Logger?.Info($"替身未匹配: trigger={triggerName}", 3);
                foreach (var n in IdleTry) if (spine.HasAnimation(n)) { sa.SetAnimation(n, false, 0); sa.AddAnimation(n, 0f, true, 0); break; }
            }
        }
        catch { }
    }
}
