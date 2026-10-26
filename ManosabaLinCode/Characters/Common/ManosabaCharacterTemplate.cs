using System;
using System.Collections.Generic;
using Godot;
using ManosabaLin.Audio.Services;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Visuals.StateMachine;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaCharacterTemplate<TCardPool, TRelicPool, TPotionPool> :
    ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>
    where TCardPool : CardPoolModel
    where TRelicPool : RelicPoolModel
    where TPotionPool : PotionPoolModel
{
    /// <summary>idle 在 SpriteFrames 里的动画名（本 mod 所有角色场景统一用 <c>default</c>）。</summary>
    private const string IdleAnimation = "default";

    /// <summary>
    /// 引擎触发名 → 场景 SpriteFrames 里的候选动画名（按序取第一个存在的）。
    /// 触发名与 <c>CreatureAnimator</c> 的常量一致：<c>Hit</c> / <c>Attack</c> / <c>Cast</c>。
    /// </summary>
    private static readonly (string Trigger, string[] Animations)[] OneShotTriggers =
    {
        ("Hit", new[] { "hit", "hurt" }),
        ("Attack", new[] { "attack" }),
        ("Cast", new[] { "cast" }),
    };

    private static readonly string[] DeathAnimations = { "die", "dead", "Dead" };

    public override string CharacterSelectSfx => $"event:/{ModId}/sfx/characters/{GetType().Name.ToLowerInvariant()}/select";

    protected override IEnumerable<string> ExtraAssetPaths =>
        base.ExtraAssetPaths.Concat(CharacterAnimationAudio.AssetPaths(this));

    /// <summary>Attach to the actual sprite at scene creation, before either animation entry point runs.</summary>
    protected NCreatureVisuals? WithAnimationAudio(NCreatureVisuals? visuals)
    {
        if (visuals == null) return null;
        try
        {
            var sprite = FindAnimatedSprite(visuals);
            if (sprite != null) CharacterAnimationAudio.Attach(sprite, this);
        }
        catch (Exception ex)
        {
            Log.Warn($"Character animation audio unavailable; visuals remain usable: {ex.Message}");
        }
        return visuals;
    }

    /// <summary>
    /// 用 <b>RitsuLib 官方接口</b>把引擎自带的动画触发器接到角色的 <see cref="AnimatedSprite2D" /> 上。
    /// <para>
    /// 引擎里 <c>CreatureCmd.TriggerAnim</c> 最终只调 <c>NCreature.SetAnimationTrigger(trigger)</c>，原版实现是
    /// <c>_spineAnimator?.SetTrigger(...)</c>；RitsuLib 的 <c>ModCreatureCombatAnimationPlaybackPatch</c> 已在该方法前拦截，
    /// 只要角色模型实现了 <c>IModCreatureCombatAnimationStateMachineFactory</c>
    /// （<c>ModCharacterTemplate</c> 已实现，本方法就是它的官方覆写点）并返回非空状态机，触发器即路由到状态机。
    /// <b>因此完全不需要自己写 Harmony 补丁。</b>
    /// </para>
    /// <para>
    /// 触发名与原版分派给 Spine 动画器的完全相同（<c>Idle</c> / <c>Hit</c> / <c>Attack</c> / <c>Cast</c> / <c>Dead</c>）。
    /// 一次性的 <c>hit</c>/<c>attack</c>/<c>cast</c> 播完由 <c>WithNext</c> 自动回到 idle（Spine 版靠多轨道叠加，
    /// AnimatedSprite2D 只有单轨，所以必须显式回 idle）；<c>Dead</c> 是终止态，不回 idle。
    /// </para>
    /// <para>
    /// 只为场景里<b>真实存在</b>的动画声明状态：没有 <c>hit</c> 动画的角色不会因此多出任何分支，
    /// 行为与改造前完全一致（触发器落空，什么也不播）。
    /// </para>
    /// </summary>
    protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(Node visualsRoot, CharacterModel character)
    {
        try
        {
            var frames = FindAnimatedSprite(visualsRoot)?.SpriteFrames;
            if (frames == null || !frames.HasAnimation(IdleAnimation))
                return null;

            var builder = ModAnimStateMachineBuilder.Create()
                .AddState(IdleAnimation, loop: true).AsInitial().Done();

            foreach (var (trigger, candidates) in OneShotTriggers)
            {
                var anim = FirstExisting(frames, candidates);
                if (anim == null)
                    continue;
                builder.AddState(anim, loop: false).WithNext(IdleAnimation);
                builder.AddAnyState(trigger, anim);
            }

            var death = FirstExisting(frames, DeathAnimations);
            if (death != null)
            {
                builder.AddState(death, loop: false).Done();
                builder.AddAnyState("Dead", death);
            }

            // BuildForVisualsRoot 会按优先级在 visualsRoot 下找后端（AnimationTree / AnimationPlayer / AnimatedSprite2D）。
            return builder.BuildForVisualsRoot(visualsRoot, character);
        }
        catch (Exception e)
        {
            // 纯视觉表现：任何失败都退回原版行为（Spine 路径自然落空），绝不影响战斗流程。
            Log.Warn($"[ManosabaLin] {GetType().Name} 战斗动画状态机创建失败，回退原版行为：{e.Message}");
            return null;
        }
    }

    private static string? FirstExisting(SpriteFrames frames, IReadOnlyList<string> candidates)
    {
        foreach (var name in candidates)
        {
            if (frames.HasAnimation(name))
                return name;
        }
        return null;
    }

    /// <summary>取角色场景里的视觉节点（优先 <c>%Visuals</c> 唯一名，退回递归查找第一个 AnimatedSprite2D）。</summary>
    private static AnimatedSprite2D? FindAnimatedSprite(Node visualsRoot)
    {
        if (!GodotObject.IsInstanceValid(visualsRoot))
            return null;

        var unique = visualsRoot.GetNodeOrNull<AnimatedSprite2D>("%Visuals");
        if (unique != null)
            return unique;

        return FindFirst(visualsRoot);
    }

    private static AnimatedSprite2D? FindFirst(Node node)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is AnimatedSprite2D sprite)
                return sprite;
            if (child == null)
                continue;
            var found = FindFirst(child);
            if (found != null)
                return found;
        }
        return null;
    }
}
