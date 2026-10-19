using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     火之时代（1 费 能力・稀有）：
///     打出时，将【夺火】【传火】【灭火】三张卡加入手牌（顺序与卡面一致）。
///     当你打出其中任意一张时，另外两张会从本场战斗中移除
///     —— 你选中的那条路，就是这一局的火之时代。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class AgeOfFire()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override int MaxUpgradeLevel => 1;
    /// <summary>
    ///     三张分支牌的效果悬浮提示。
    ///     本来这里是 <c>HoverTipFactory.FromCard&lt;T&gt;(false)</c> 的牌面预览，但传火/灭火/夺火
    ///     改成「卡面只留风味文本」后（描述只剩 1 / 2 / 3），预览里也只会显示「1 / 2 / 3」，
    ///     看不到实际效果 ⇒ 改为直接用它们的 <c>_EFFECT</c> 键生成效果提示框。
    ///     <para>
    ///         ⚠️ 升级后的火之时代给出的三张分支牌也是**升级版** ⇒ 提示必须按「这张牌升级后的样子」解析：
    ///         <c>{IfUpgraded:show:A|B}</c> 要走 A 分支，数值也要是升级值（传火 格挡 8→11、
    ///         夺火 王魂系数 3→5）。所以这里不能再用规范模型（规范模型恒为未升级）。
    ///     </para>
    /// </summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return BranchEffectTip<UsurpTheFlame>("MANOSABA_LIN_CARD_USURP_THE_FLAME_EFFECT");
            yield return BranchEffectTip<LinkTheFire>("MANOSABA_LIN_CARD_LINK_THE_FIRE_EFFECT");
            yield return BranchEffectTip<ExtinguishFlame>("MANOSABA_LIN_CARD_EXTINGUISH_FLAME_EFFECT");
        }
    }

    /// <summary>
    ///     按「打出后会真正拿到的那张分支牌」生成效果提示（升级时就是升级版）。
    ///     做法：拿规范模型拷一份<b>可变副本</b>（<c>ToMutable</c> —— 刻意不用
    ///     <c>CombatState.CreateCard</c>，后者会把牌注册进战斗、而这个属性会被反复求值），
    ///     就地跑一遍和真实升级完全相同的流程（<c>UpgradeInternal</c> + <c>FinalizeUpgradeInternal</c>，
    ///     即 <c>CardCmd.Upgrade</c> 去掉特效的部分），再交给 <see cref="CardEffectHoverTipFactory" />。
    /// </summary>
    private IHoverTip BranchEffectTip<T>(string locEntry) where T : CardModel
    {
        if (!IsUpgraded)
            return CardEffectHoverTipFactory.FromCard<T>(locEntry);

        try
        {
            var preview = ModelDb.Card<T>().ToMutable();
            preview.UpgradeInternal();
            preview.FinalizeUpgradeInternal();

            // 【夺火】的伤害随发夹里的计数成长（升级系数 3→5）⇒ 同步成「入手时会看到的数值」。
            if (preview is UsurpTheFlame && Owner is { } owner)
                UsurpTheFlame.SyncDisplayedDamage(preview, owner);

            return CardEffectHoverTipFactory.FromCard(preview, locEntry);
        }
        catch (Exception e)
        {
            Log.Warn($"[AgeOfFire] 升级版分支牌预览生成失败，回落未升级提示：{e.Message}");
            return CardEffectHoverTipFactory.FromCard<T>(locEntry);
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner || CombatState is not { } combatState)
            return;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 加入顺序与卡面描述一致：【夺火】(黄) →【传火】(绿) →【灭火】(蓝)。
        var branches = new CardModel[]
        {
            combatState.CreateCard<UsurpTheFlame>(owner),
            combatState.CreateCard<LinkTheFire>(owner),
            combatState.CreateCard<ExtinguishFlame>(owner)
        };

        foreach (var branch in branches)
        {
            // 升级后的火之时代，给出的三张分支牌也是升级版本。
            if (IsUpgraded)
                CardCmd.Upgrade(branch, CardPreviewStyle.None);

            // 【夺火】卡面伤害随「计数」成长 ⇒ 建卡时同步显示值，免得入手时数字还是旧的。
            if (branch is UsurpTheFlame)
                UsurpTheFlame.SyncDisplayedDamage(branch, owner);

            await CardPileCmd.AddGeneratedCardToCombat(branch, PileType.Hand, owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级后费用 +1（1 → 2）。
        EnergyCost.UpgradeBy(1);
    }

    /// <summary>
    ///     打出三张分支牌中的任意一张时，把另外两张（所有同名副本）从本场战斗中移除。
    ///     只处理打出者自己的牌堆，不会动队友手里的副本。
    /// </summary>
    internal static async Task RemoveOtherBranches(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel played)
    {
        var others = owner.Piles
            .SelectMany(static pile => pile.Cards)
            .Where(card => !ReferenceEquals(card, played) && IsBranch(card))
            .ToList();

        foreach (var other in others)
            await CardPileCmd.RemoveFromCombat(other);
    }

    private static bool IsBranch(CardModel card)
    {
        return card is LinkTheFire or ExtinguishFlame or UsurpTheFlame;
    }
}
