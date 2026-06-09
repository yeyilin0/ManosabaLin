using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 情绪球体基类：效果直接写在球体里，球体消散效果消失。
/// </summary>
/// <typeparam name="T">与之关联的牌的类型，将显示为情绪球体的视觉效果</typeparam>
public abstract class EmotionOrb<T> : ModOrbTemplate, IEmotionOrb where T : CardModel
{
    public override decimal PassiveVal => 0;
    public override decimal EvokeVal => 0;
    public override Color DarkenedColor => OrbColor;

    protected abstract Color OrbColor { get; }

    public override OrbAssetProfile AssetProfile => new(
        IconPath: "res://images/events/crystal_sphere/crystal_sphere_rare_card_reward.png",
        VisualsScenePath: "res://ManosabaLin/scenes/orbs/orb_visuals/orb_blank.tscn"
    );

    protected override Node2D? TryCreateOrbSprite()
        => RitsuGodotNodeFactories.CreateFromScenePath<Node2D>(AssetProfile.VisualsScenePath!);

    public CardModel GetEmotionCard()
    {
        return ModelDb.Card<T>();
    }

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
        return Task.FromResult<IEnumerable<Creature>>([Owner.Creature]);
    }
}

public interface IEmotionOrb
{
    CardModel GetEmotionCard();
}

[HarmonyPatch(typeof(NOrb), nameof(NOrb._Ready))]
internal static class EmotionOrbVisualPatch
{
    [HarmonyPostfix]
    public static void Postfix(NOrb __instance)
    {
        var orb = __instance.Model;
        if (orb is not IEmotionOrb emotionOrb) return;
        var card = emotionOrb.GetEmotionCard();
        var nCard = NCard.Create(card);
        if (nCard == null) return;

        nCard.Modulate = nCard.Modulate with { A = 0 };
        __instance.AddChildSafely(nCard);
        nCard.UpdateVisuals(PileType.None, CardPreviewMode.Normal);

        nCard.Scale = new Vector2(0.5f, 0.5f);

        nCard._descriptionLabel.QueueFree();
        nCard._ancientTextBg.QueueFree();
        nCard._typePlaque.QueueFree();
        nCard._typeLabel.QueueFree();
        nCard._titleLabel.QueueFree();
        nCard._ancientBanner.QueueFree();
        nCard._energyIcon.QueueFree();
        nCard._energyLabel.QueueFree();

        var tween = __instance.CreateTween();

        tween.TweenProperty(nCard, "modulate:a", 1.0f, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}
