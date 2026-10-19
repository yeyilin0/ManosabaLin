using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     灭火（2 费 能力・先古・Lin 卡池）：
///     世界之火熄灭 —— 你与所有敌人造成的伤害减半，且你的格挡不再于回合开始时消失。
///     升级后额外在每回合开始时获得 15 点格挡。
///     由「火之时代」获得；打出后另外两张分支牌被移除。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class ExtinguishFlame()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_EXTINGUISH_FLAME_EFFECT";

    /// <summary>
    ///     卡面只留风味文本，效果改走悬浮提示（与「我不听，我需要你」同款）。
    ///     提示文案里的 `{IfUpgraded:...}` 由 <c>CardEffectHoverTipFactory</c> 注入 <c>IfUpgradedVar</c> 解析。
    /// </summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner)
            return;

        await AgeOfFire.RemoveOtherBranches(choiceContext, owner, this);

        // 【灭火】能力只保留 1 个实例：Amount 仅作「已存在」标记，
        // 真正的效果（减半 / 格挡不消失 / 升级后 15 格挡）由 CardUpgraded 决定。
        var power = await PowerCmd.Apply<ExtinguishFlamePower>(
            choiceContext, owner.Creature, 1, owner.Creature, this, false);
        if (power is not null)
            power.CardUpgraded = IsUpgraded;
    }
}
