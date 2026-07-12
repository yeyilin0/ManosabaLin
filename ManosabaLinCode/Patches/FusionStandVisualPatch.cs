using Godot;
using HarmonyLib;
using ManosabaLin.Characters.Hiro.Monsters;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
public static class FusionStandVisualPatch
{
    static FusionStandVisualPatch()
    {
        FusionStandManager.StandActivated += TryCreateStand;
    }

    [HarmonyPostfix]
    public static void Postfix(NCreature __instance)
    {
        try
        {
            var creature = __instance.Entity;
            if (creature?.Monster != null && creature.GetPower<FusionStandPower>() != null)
                TryCreateStand(creature.Monster);
        }
        catch
        {
            // Visual patch should never block combat setup.
        }
    }

    private static readonly string[] IdleAnimations =
    [
        "idle_loop", "Idle_Loop", "idle", "Idle", "idle1", "stand", "animation"
    ];

    internal static void TryCreateStand(MonsterModel monster)
    {
        try
        {
            var creature = monster.Creature;
            if (creature == null || creature.GetPower<FusionStandPower>() == null) return;

            var creatureNode = creature.GetCreatureNode();
            if (creatureNode == null) return;
            if (creatureNode.GetNodeOrNull<NCreatureVisuals>("FusionStand") != null) return;

            var stand = monster.CreateVisuals();
            stand.Name = "FusionStand";
            stand.ShowBehindParent = true;
            stand.Modulate = new Color(1.0f, 0f, 0f, 0.5f);
            stand.Scale = new Vector2(1.05f, 1.05f);
            stand.Position = new Vector2(80f, -60f);
            stand.SetProcess(true);
            stand.SetPhysicsProcess(true);

            creatureNode.AddChild(stand);
            stand.SetUpSkin(monster);
            stand.UpdatePhobiaMode(monster);
            DisableStandTargetingControls(stand);
            ApplyStandPhaseVisual(monster, stand);
            PlayFirstAnimation(stand, IdleAnimations, loop: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[FusionStand] visual failed: {ex.Message}");
        }
    }

    private static void DisableStandTargetingControls(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = Control.MouseFilterEnum.Ignore;

            if (child is Node childNode)
                DisableStandTargetingControls(childNode);
        }
    }

    private static void ApplyStandPhaseVisual(MonsterModel monster, NCreatureVisuals stand)
    {
        if (monster is not GuardThreeMonster) return;

        var body = stand.GetNodeOrNull<Sprite2D>("%Visuals");
        if (body == null)
        {
            try
            {
                body = stand.GetCurrentBody() as Sprite2D;
            }
            catch
            {
                return;
            }
        }
        if (body == null) return;

        var texture = PreloadManager.Cache.GetTexture2D("guard_three_phase2.png".MonstersImagePath());
        if (texture != null)
        {
            body.Texture = texture;
        }
    }

    internal static void PlayFirstAnimation(NCreatureVisuals visuals, IReadOnlyList<string> candidates, bool loop)
    {
        if (visuals.SpineBody == null) return;

        foreach (var animation in candidates)
        {
            if (!visuals.SpineBody.HasAnimation(animation)) continue;

            var spine = new SpineAnimationAccess(visuals.SpineBody);
            spine.SetAnimation(animation, loop, 0);
            return;
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim),
    typeof(Creature), typeof(string), typeof(float))]
public static class FusionStandAnimationSyncPatch
{
    private static readonly string[] IdleAnimations = ["idle_loop", "Idle_Loop", "idle", "Idle"];
    private static readonly string[] AttackAnimations =
    [
        "attack", "Attack", "attack1", "attack_1", "AttackTrigger", "swing", "swing_1", "swing_2",
        "beat", "slash", "claw", "bite", "smash", "punch", "shoot", "stab"
    ];
    private static readonly string[] CastAnimations =
    [
        "cast", "Cast", "spell", "buff", "roar", "sharpen", "powerup", "PowerUp", "channel",
        "summon", "taunt", "howl", "scream", "shout", "battlecry"
    ];
    private static readonly string[] HitAnimations = ["hit", "Hit", "hurt", "Hurt", "damage", "hit1", "damaged"];
    private static readonly string[] DeathAnimations = ["die", "Die", "death", "Death", "dead", "Dead"];

    [HarmonyPostfix]
    public static void Postfix(Creature creature, string triggerName)
    {
        if (creature.IsPlayer) return;

        try
        {
            var creatureNode = creature.GetCreatureNode();
            var stand = creatureNode?.GetNodeOrNull<NCreatureVisuals>("FusionStand");
            if (stand?.SpineBody == null) return;

            var candidates = triggerName.ToLowerInvariant() switch
            {
                "attack" or "attacktrigger" => AttackAnimations,
                "cast" or "powerup" => CastAnimations,
                "hit" => HitAnimations,
                "dead" or "die" => DeathAnimations,
                _ => null
            };
            if (candidates == null) return;

            var spine = new SpineAnimationAccess(stand.SpineBody);
            var animation = candidates.FirstOrDefault(stand.SpineBody.HasAnimation);
            if (animation == null)
            {
                FusionStandVisualPatch.PlayFirstAnimation(stand, IdleAnimations, loop: true);
                return;
            }

            spine.SetAnimation(animation, false, 0);

            var idle = IdleAnimations.FirstOrDefault(stand.SpineBody.HasAnimation);
            if (idle != null)
                spine.AddAnimation(idle, 0f, true, 0);
        }
        catch
        {
            // Animation sync is best-effort only.
        }
    }
}
