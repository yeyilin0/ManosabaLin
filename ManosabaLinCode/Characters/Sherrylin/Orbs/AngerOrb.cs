using Godot;
using ManosabaLin.Extensions;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 愤怒球体：挂载后获得2层橘雪莉的魔法，下回合消散。
/// </summary>
[RegisterOrb]
public class AngerOrb : ModOrbTemplate
{
    public PowerModel? BoundPower { get; set; }

    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;
    public override Color DarkenedColor => new(1f, 0.3f, 0.3f);

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/orbs/anger_orb.png",
        VisualsScenePath: "res://ManosabaLin/scenes/orbs/orb_visuals/anger_orb.tscn"
    );

    protected override Node2D? TryCreateOrbSprite()
        => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        // 施加2层橘雪莉的魔法
        await PowerCmd.Apply<XlmPower>(
            ctx, Owner.Creature, 2,
            Owner.Creature, null, false);

        // 消散
        await OrbCmd.EvokeNext(ctx, Owner);
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;

    public override Task<IEnumerable<Creature>> Evoke(PlayerChoiceContext ctx)
    {
        return Task.FromResult<IEnumerable<Creature>>(new[] { Owner.Creature });
    }
}
