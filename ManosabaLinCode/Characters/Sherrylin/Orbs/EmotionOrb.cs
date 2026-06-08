using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 情绪球体基类：效果直接写在球体里，球体消散效果消失。
/// </summary>
public abstract class EmotionOrb : ModOrbTemplate
{
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;
    public override Color DarkenedColor => GetOrbColor();

    protected abstract Color GetOrbColor();
    protected abstract string GetOrbName();

    public override OrbAssetProfile AssetProfile => new(
        IconPath: $"res://ManosabaLin/images/orbs/{GetOrbName()}.png",
        VisualsScenePath: $"res://ManosabaLin/scenes/orbs/orb_visuals/{GetOrbName()}.tscn"
    );

    protected override Node2D? TryCreateOrbSprite()
        => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    /// <summary>
    /// 下回合开始自动消散
    /// </summary>
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        await OrbCmd.EvokeNext(ctx, Owner);
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;

    public override Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext ctx)
    {
        return Task.FromResult<IEnumerable<Creature>>(new[] { Owner.Creature });
    }
}
