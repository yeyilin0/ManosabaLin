using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     夺火（1 费 攻击・先古・Lin 卡池）。
///     伤害 = 5 + 计数 × 3（升级后 × 5）；计数由发夹（初始遗物）保存，每场战斗第 1 次打出时 +1，整局持续成长。
///     <b>右键强化</b>：消耗 4 点计数把本卡强化；强化后打出可额外获得 1 个额外回合。
///     额外回合走 <b>原版</b> <c>AbstractModel.ShouldTakeExtraTurn</c> / <c>AfterTakingExtraTurn</c>
///     （与「佩尔之眼」同款），不写任何 Harmony patch。
///     <para>
///     <b>升级效果</b>：移除全体友方至多 20 点格挡，并等量转化为【临时生命】
///     （见 <see cref="ConvertBlockToTempHp" /> 与 <see cref="Powers.TempHpPower" />）。
///     </para>
///     可重复打出（不消耗，打出后进弃牌堆）；由「火之时代」获得；打出后另外两张分支牌被移除。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class UsurpTheFlame()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy), IEasyRightClickableCard
{
    private const int BaseDamageFlat = 5;
    private const int BaseDamagePerCount = 3;
    private const int UpgradedDamagePerCount = 5;

    /// <summary>右键「强化」消耗的夺火计数。</summary>
    public const int StrengthenCountCost = 4;

    /// <summary>升级效果里每名友方最多被转化的格挡数（本地化口径「至多20点」）。</summary>
    private const int MaxBlockConverted = 20;

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(BaseDamageFlat, ValueProp.Move)];

    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_USURP_THE_FLAME_EFFECT";

    /// <summary>右键「强化」标记：强化后打出，本卡额外给出 1 个额外回合。</summary>
    [SavedProperty] public bool Strengthened { get; private set; }

    /// <summary>卡面只留风味文本，效果改走悬浮提示（与「我不听，我需要你」同款）。</summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry); }
    }

    private YalisalinsHairpin? Hairpin => Owner?.Relics.OfType<YalisalinsHairpin>().FirstOrDefault();

    // —— 右键「强化」：消耗 4 点计数，把本卡标记为已强化（强化后打出才给额外回合）——

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        if (Strengthened)
            return false;
        if (Pile?.Type != PileType.Hand)
            return false;
        return Hairpin is { } hairpin && hairpin.UsurpTheFlameCount >= StrengthenCountCost;
    }

    public Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (Strengthened || Pile?.Type != PileType.Hand)
            return Task.CompletedTask;
        if (Hairpin is not { } hairpin || !hairpin.TrySpendUsurpTheFlameCount(StrengthenCountCost))
            return Task.CompletedTask;

        Strengthened = true;
        RefreshCardVisuals();
        return Task.CompletedTask;
    }

    private void RefreshCardVisuals()
    {
        var node = NCard.FindOnTable(this);
        node?.UpdateVisuals(Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner || cardPlay.Target is not { } target)
            return;

        await AgeOfFire.RemoveOtherBranches(choiceContext, owner, this);

        // 读出发夹里的夺火计数（每场战斗第 1 次打出时 +1）。
        var hairpin = owner.Relics.OfType<YalisalinsHairpin>().FirstOrDefault();
        var count = hairpin?.UsurpTheFlameCount ?? 0;
        var dmg = BaseDamageFlat + count * (IsUpgraded ? UpgradedDamagePerCount : BaseDamagePerCount);

        // 本击结算完再让计数 +1：本场第 1 次打出享受「基数 0」的伤害（5），成长在后续战斗才体现。
        hairpin?.GainUsurpTheFlameCountOnce();

        // 同步给悬浮提示用到的 DynamicVars，避免卡面显示与实战不一致。
        DynamicVars.Damage.BaseValue = dmg;

        await DamageCmd.Attack(dmg)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 升级效果：「移除全体友方至多20点格挡并转换为临时生命」。
        if (IsUpgraded)
            await ConvertBlockToTempHp(choiceContext, owner);

        // 强化后打出 ⇒ 本卡额外给 1 个额外回合（由发夹覆写的原版 ShouldTakeExtraTurn 在回合结束时发放）。
        if (Strengthened)
        {
            Strengthened = false;
            hairpin?.QueueUsurpExtraTurn();
        }
    }

    /// <summary>
    ///     升级效果：每名存活友方各移除至多 <see cref="MaxBlockConverted" /> 点格挡，
    ///     并等量转化为【临时生命】（<see cref="TempHpPower" />，Counter 堆叠 ⇒ 已有临时生命直接累加）。
    ///     友方口径 = <see cref="CombatState.Allies" /> 中的存活者（联机时含队友角色）。
    /// </summary>
    private static async Task ConvertBlockToTempHp(PlayerChoiceContext choiceContext, Player owner)
    {
        var combatState = owner.Creature.CombatState;
        if (combatState == null)
            return;

        foreach (var ally in combatState.Allies)
        {
            if (ally.IsDead)
                continue;

            var take = Math.Min(ally.Block, MaxBlockConverted);
            if (take <= 0)
                continue;

            await CreatureCmd.LoseBlock(choiceContext, ally, take, null);
            await PowerCmd.Apply<TempHpPower>(choiceContext, ally, take, owner.Creature, null);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级后伤害系数变为 5：把 DynamicVars 显示值对齐到「当前计数下」的升级公式，
        // 实战伤害仍在 OnPlay 按实时计数计算。
        var hairpin = Owner?.Relics.OfType<YalisalinsHairpin>().FirstOrDefault();
        var count = hairpin?.UsurpTheFlameCount ?? 0;
        DynamicVars.Damage.BaseValue = BaseDamageFlat + count * UpgradedDamagePerCount;
    }

    /// <summary>
    ///     把「当前计数」算出的伤害同步到卡面显示值。
    ///     由【火之时代】在建出夺火时调用——卡是当场生成的，显示值默认只有基数（5），
    ///     不同步的话入手后数字是旧的；实战伤害仍在 <see cref="OnPlay" /> 按实时计数计算。
    /// </summary>
    internal static void SyncDisplayedDamage(CardModel card, Player owner)
    {
        var hairpin = owner.Relics.OfType<YalisalinsHairpin>().FirstOrDefault();
        var count = hairpin?.UsurpTheFlameCount ?? 0;
        card.DynamicVars.Damage.BaseValue =
            BaseDamageFlat + count * (card.IsUpgraded ? UpgradedDamagePerCount : BaseDamagePerCount);
    }
}
