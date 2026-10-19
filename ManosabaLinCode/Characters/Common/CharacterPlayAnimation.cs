using System.Collections.Generic;
using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ManosabaLin.Characters.Common;

/// <summary>
/// 让使用 AnimatedSprite2D（而非 Spine 骨骼）的角色在打牌时播放出牌动画。
/// <para>
/// 原版角色（铁甲战士等）的攻击/施法动画由卡牌 <c>OnPlay</c> 里的
/// <c>CreatureCmd.TriggerAnim</c> 驱动 Spine 骨骼；mod 角色没有 Spine，
/// 这里用角色场景 SpriteFrames 里的同名动画（attack / cast）替代，
/// 播完自动回到 default（idle 循环）。
/// </para>
/// <para>
/// 因为 idle 与 attack 的关键姿势不一致（垂手站立 ↔ 举手挥击），直接切换会「跳帧」。
/// 这里在切换时把「切换瞬间的旧画面」复制成一个半透明替身盖在上层做快速淡出，
/// 形成交叉溶解，把姿势跳变藏掉。
/// </para>
/// </summary>
public static class CharacterPlayAnimation
{
    /// <summary>每个 AnimatedSprite2D 只连一次 animation_finished 回调。</summary>
    private static readonly Dictionary<ulong, Callable> IdleReturn = new();

    /// <summary>每个 AnimatedSprite2D 当前正在淡出的「旧画面替身」。</summary>
    private static readonly Dictionary<ulong, Node2D> Ghosts = new();

    /// <summary>交叉溶解时长（秒）。太短盖不住跳变，太长会拖影。</summary>
    private const float CrossFadeSeconds = 0.12f;

    /// <summary>
    /// 按卡牌类型播放对应的出牌动画。
    /// <para>
    /// <c>Attack</c> → <c>attack</c>；**只有 <c>Power</c>（能力牌）** → <c>cast</c>。
    /// <c>Skill</c> 不播任何出牌动画（施法动画只属于能力牌，技能牌保持待机）。
    /// </para>
    /// </summary>
    public static void PlayByCardType(Creature? creature, CardType type)
    {
        if (creature == null)
            return;

        var anim = type switch
        {
            CardType.Attack => "attack",
            CardType.Power => "cast",
            _ => null
        };
        if (anim == null)
            return;

        Play(creature, anim);
    }

    /// <summary>让 creature 的视觉节点播放指定动画（若该动画存在）。</summary>
    public static void Play(Creature creature, string anim)
    {
        try
        {
            var sprite = creature.GetCreatureNode()?.Visuals?.GetNodeOrNull<AnimatedSprite2D>("%Visuals");
            if (sprite?.SpriteFrames == null)
                return;
            if (!sprite.SpriteFrames.HasAnimation(anim))
                return;
            if (sprite.Animation == anim && sprite.IsPlaying())
                return;

            EnsureIdleReturn(sprite);
            CrossFade(sprite, anim);
        }
        catch
        {
            // 纯视觉表现，任何失败都不应影响战斗流程。
        }
    }

    /// <summary>把「切换瞬间的旧画面」复制成替身盖在上层淡出，再让本体切到新动画。</summary>
    private static void CrossFade(AnimatedSprite2D sprite, string anim)
    {
        var parent = sprite.GetParent();
        var id = sprite.GetInstanceId();

        // 清掉上一次还没淡完的替身，避免叠影。
        if (Ghosts.TryGetValue(id, out var prev) && GodotObject.IsInstanceValid(prev))
            prev.QueueFree();
        Ghosts.Remove(id);

        if (parent != null)
        {
            var ghost = new AnimatedSprite2D
            {
                SpriteFrames = sprite.SpriteFrames,
                Centered = sprite.Centered,
                FlipH = sprite.FlipH,
                FlipV = sprite.FlipV,
                Offset = sprite.Offset,
                Position = sprite.Position,
                Scale = sprite.Scale,
                Rotation = sprite.Rotation,
                Modulate = sprite.Modulate,
                ZIndex = sprite.ZIndex,
                ZAsRelative = sprite.ZAsRelative,
            };
            ghost.Animation = sprite.Animation;
            ghost.Stop();
            ghost.Frame = sprite.Frame;

            parent.AddChild(ghost);
            parent.MoveChild(ghost, sprite.GetIndex() + 1); // 排在 Visuals 之后 => 画在上层
            Ghosts[id] = ghost;

            var tween = ghost.CreateTween();
            tween.TweenProperty(ghost, "modulate:a", 0.0f, CrossFadeSeconds);
            tween.TweenCallback(Callable.From(() =>
            {
                if (Ghosts.TryGetValue(id, out var g) && g == ghost)
                    Ghosts.Remove(id);
                if (GodotObject.IsInstanceValid(ghost))
                    ghost.QueueFree();
            }));
        }

        sprite.Play(anim);
    }

    private static void EnsureIdleReturn(AnimatedSprite2D sprite)
    {
        var id = sprite.GetInstanceId();
        if (IdleReturn.ContainsKey(id))
            return;

        var callable = Callable.From(() => ReturnToIdle(sprite));
        IdleReturn[id] = callable;
        sprite.Connect(AnimatedSprite2D.SignalName.AnimationFinished, callable);
    }

    private static void ReturnToIdle(AnimatedSprite2D sprite)
    {
        if (!GodotObject.IsInstanceValid(sprite))
        {
            IdleReturn.Remove(sprite.GetInstanceId());
            return;
        }

        if (sprite.SpriteFrames?.HasAnimation("default") == true)
            CrossFade(sprite, "default");
    }
}
