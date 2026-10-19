using ManosabaLin.Characters.Yalisalin.Components;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     传火（1 费 技能・先古・Lin 卡池）：
///     把火种撒进牌堆 —— 每次打出随机点亮 1 张牌（挂上【伟大灵魂】）；
///     同时存在的【伟大灵魂】至多 4 个（恒定上限，升级不提高，见 <see cref="MaxFireSeeds" />）。
///     <para>
///     <b>点火</b>：打出时消耗当前<b>王魂数</b>（＝上限，见 <see cref="SoulCap" />），
///     把它随机分拆成若干份（每份 1~3，所有分拆方案等权），每一份触发一次点火：
///     1 → 火星效果库、2 → 火势效果库、3 → 燎原效果库，每次从对应库随机摇<b>一项</b>具体效果。
///     分拆不消耗王魂计数；本牌成长由效果库里的「灵魂满载」「先古的火种」
///     与火种（【伟大灵魂】）打出时推动（见 <see cref="GreatSoul" />）。
///     详见 <see cref="LinkTheFireEffects" />。
///     </para>
///     带【伟大灵魂】的牌打出时使本牌成长 1 次（不燃尽，见 <see cref="GreatSoul" />）。
///     <para>
///     <b>升级效果</b>：随机给予一名友方 12 次 1 点格挡，若其格挡为友方最低则使其额外恢复 1 点生命
///     （见 <see cref="GrantAllyBlockAndMaybeHeal" />）。
///     </para>
///     可重复打出（不消耗，打出后进弃牌堆）；由「火之时代」获得；打出后另外两张分支牌被移除。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class LinkTheFire()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    /// <summary>王魂数的基数（升级 +1；火种打出与效果库的成长再累加上去）。</summary>
    private const int BaseSoulCap = 4;

    private const int UpgradedSoulCapBonus = 1;

    /// <summary>火种（【伟大灵魂】）的同时存在上限：恒定 4，升级不提高（本地化口径「至多同时存在4个」）。</summary>
    private const int MaxFireSeeds = 4;

    /// <summary>升级效果给友方格挡的次数（每次 1 点，分开结算是为了逐次触发格挡获得类钩子）。</summary>
    private const int UpgradedBlockApplications = 12;

    /// <summary>卡面/悬浮提示里显示当前火种上限的动态数值键。</summary>
    private const string SoulCapKey = "SoulCap";

    private int _growth;

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    /// <summary>本场战斗中【传火】的成长次数：由效果库里的「灵魂满载」「先古的火种」与火种打出推动。</summary>
    [SavedProperty]
    public int Growth
    {
        get => _growth;
        private set => _growth = Math.Max(0, value);
    }

    /// <summary>当前<b>王魂数</b>：打出时按它分拆点火；点亮火种每次 1 张、总量至多 <see cref="MaxFireSeeds" /> 个（升级不影响点亮数）。</summary>
    public int SoulCap => BaseSoulCap + (IsUpgraded ? UpgradedSoulCapBonus : 0) + Growth;

    /// <summary>本牌成长 <paramref name="times" /> 次（王魂数 +N，联机安全：Growth 是 SavedProperty）。</summary>
    internal void Grow(int times = 1)
    {
        if (times <= 0)
            return;

        Growth += times;
    }

    /// <summary>
    ///     让玩家所有的【传火】各成长 <paramref name="times" /> 次（王魂数 +N）。
    ///     火种（【伟大灵魂】）打出时调用 —— 本地化口径「打出此牌时，使【传火】本场战斗内成长1次」。
    ///     多张【传火】同时成长以保持王魂数一致；联机安全：Growth 是 [SavedProperty]。
    /// </summary>
    internal static void GrowAll(Player owner, int times = 1)
    {
        if (times <= 0)
            return;

        foreach (var card in AllCards(owner))
        {
            if (card is LinkTheFire fire)
                fire.Grow(times);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0m),
        new CalculationExtraVar(1m),
        new CalculatedVar(SoulCapKey).WithMultiplier((card, _) => ((LinkTheFire)card).SoulCap)
    ];

    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_LINK_THE_FIRE_EFFECT";

    /// <summary>卡面只留风味文本，效果改走悬浮提示（与「我不听，我需要你」同款）。</summary>
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

        // 点亮数恒为 1 张：「点亮所有牌中随机1张牌」；火种同时存在上限恒为 4（升级不加）。
        var lit = AllCards(owner).Count(static card => GreatSoul.Has(card));
        FillUpFireSeeds(owner, Math.Min(lit + 1, MaxFireSeeds));

        // 升级效果：「随机给予友方12次1点格挡，若其格挡为友方最低则使其额外恢复1点生命」。
        if (IsUpgraded)
            await GrantAllyBlockAndMaybeHeal(owner, cardPlay);

        await LinkTheFireEffects.Ignite(choiceContext, cardPlay, this, SoulCap);
    }

    /// <summary>
    ///     随机点亮牌堆中的牌，直到场上的【伟大灵魂】数量达到 <paramref name="cap" />。
    ///     调用方传 <c>Math.Min(当前火种数 + 1, MaxFireSeeds)</c>：每次打出只点亮 1 张，总量至多 4 个。
    /// </summary>
    private static void FillUpFireSeeds(Player owner, int cap)
    {
        var all = AllCards(owner).ToList();
        var need = cap - all.Count(static card => GreatSoul.Has(card));
        if (need <= 0)
            return;

        var candidates = all.Where(static card => !GreatSoul.Has(card)).ToList();
        if (candidates.Count == 0)
            return;

        // Fisher-Yates 只洗前 take 个位置：随机挑出要点的牌，不做无谓的全量洗牌。
        var rng = owner.RunState.Rng.CombatCardSelection;
        var take = Math.Min(need, candidates.Count);
        for (var i = 0; i < take; i++)
        {
            var j = i + rng.NextInt(candidates.Count - i);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            GreatSoul.TryLightUp(candidates[i]);
        }
    }

    /// <summary>
    ///     升级效果：随机挑一名存活友方，分 <see cref="UpgradedBlockApplications" /> 次各给 1 点格挡；
    ///     给完之后若其格挡仍是友方最低（并列最低也算），额外恢复 1 点生命。
    ///     <para>
    ///     友方口径 = <see cref="CombatState.Allies" /> 中的存活者（联机时含队友角色）；
    ///     随机走 <c>CombatTargets</c>（与发夹选敌同一股随机数，两端一致执行故选出的友方相同）。
    /// </para>
    /// </summary>
    private static async Task GrantAllyBlockAndMaybeHeal(Player owner, CardPlay cardPlay)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return;

        var allies = combatState.Allies.Where(static c => !c.IsDead).ToList();
        if (allies.Count == 0)
            return;

        var target = owner.RunState.Rng.CombatTargets.NextItem(allies);
        for (var i = 0; i < UpgradedBlockApplications; i++)
            await CreatureCmd.GainBlock(target, 1m, ValueProp.Move, cardPlay);

        if (target.Block <= allies.Min(static c => c.Block))
            await CreatureCmd.Heal(target, 1m);
    }

    /// <summary>本场战斗里玩家手牌 / 抽牌堆 / 弃牌堆中的全部牌。</summary>
    private static IEnumerable<CardModel> AllCards(Player owner)
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(owner).Cards)
                yield return card;
        }
    }
}
