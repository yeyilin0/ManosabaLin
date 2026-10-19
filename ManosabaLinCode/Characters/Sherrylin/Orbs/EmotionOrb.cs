using HarmonyLib;
using ManosabaLin.Audio.Services;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
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

    /// <summary>
    /// 引擎在 orb 被引导（channel）时会调 <see cref="OrbModel.PlayChannelSfx"/>：
    /// <c>ChannelSfx != ""</c> 走 FMOD，否则回退到
    /// <c>res://debug_audio/{Id.Entry.ToLowerInvariant()}_channel.mp3</c>。
    /// 我们没有那个 mp3，不覆写的话每引导一颗情绪球都会刷一条
    /// "No loader found for resource"（而且完全没声音）。
    /// 这里按 mod 既有做法（<see cref="CardAudioService"/> 那套「先探测再选用」）挑一个真实存在的事件；
    /// 列表末尾是原版 orb 自用的通道音，必然存在，用作兜底。
    /// </summary>
    protected override string ChannelSfx => _channelSfx ??= PickChannelSfx();

    private static string? _channelSfx;

    private static readonly string[] ChannelSfxCandidates =
    [
        "event:/sfx/buff",
        "event:/sfx/ui/gain_energy",
        "event:/sfx/characters/defect/defect_lightning_channel",
    ];

    private static string PickChannelSfx()
    {
        foreach (var candidate in ChannelSfxCandidates)
            if (FmodHelper.IsEventExists(candidate))
                return candidate;
        return ChannelSfxCandidates[^1];
    }

    /// <summary>
    /// 球体画面上渲染的就是那张情绪卡（见 <see cref="EmotionOrbVisualPatch" />），
    /// 因此鼠标碰上去时把那张卡本身的预览一并挂出来 —— 挂在血条下方的球一样能看效果。
    /// </summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromCard<T>()];

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
