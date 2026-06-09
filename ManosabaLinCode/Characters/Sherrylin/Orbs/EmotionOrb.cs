using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 情绪球体基类：球体只负责能力的获得与移除。
/// 获得球体 → 获得能力；球体被顶掉或消散 → 能力一起移除。
/// </summary>
public abstract class EmotionOrb : ModOrbTemplate
{
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;
    public override Color DarkenedColor => GetOrbColor();

    /// <summary>
    /// 绑定的能力引用，球体消散时一起移除。
    /// </summary>
    public PowerModel? BoundPower { get; set; }

    protected abstract Color GetOrbColor();
    protected abstract string GetOrbName();

    public override OrbAssetProfile AssetProfile => new(
        IconPath: $"res://ManosabaLin/images/orbs/{GetOrbName()}.png",
        VisualsScenePath: $"res://ManosabaLin/scenes/orbs/orb_visuals/{GetOrbName()}.tscn"
    );

    protected override Node2D? TryCreateOrbSprite()
        => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    /// <summary>
    /// 下回合开始自动消散，连带能力一起移除。
    /// </summary>
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        await RemoveBoundPower();
        await OrbCmd.EvokeNext(ctx, Owner);
    }

    /// <summary>
    /// 消散时移除绑定的能力。
    /// </summary>
    public override async Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext ctx)
    {
        await RemoveBoundPower();
        return new[] { Owner.Creature };
    }

    private async Task RemoveBoundPower()
    {
        if (BoundPower != null)
        {
            await PowerCmd.Remove(BoundPower);
            BoundPower = null;
        }
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}
