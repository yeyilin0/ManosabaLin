using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Yalisalin.Components;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     紫藤亚里沙的坏结局（先古诅咒）。
///     <para>
///         毒化一：只要「所有牌」（手牌 + 抽牌堆 + 弃牌堆）里带着此牌，
///         <b>火色无法触发消耗效果</b> —— 判据在
///         <see cref="Relics.YalisalinsHairpin.IsFireColorConsumeSuppressed" />。
///     </para>
///     <para>
///         毒化二：回合结束时手牌中每有 1 张【余火】牌，失去 1 点生命，并获得 3 层【嫌疑】。
///     </para>
///     其余与另外四张坏结局一致：丢弃 / 消耗都会回到手牌，打出时处刑（生成死亡卡并自伤 999）。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class YalisalinBadEnding : ManosabaCardTemplate
{
    protected override IEnumerable<ICardComponent> CanonicalComponents => [new YalisalinDeath()];

    private const int DirectDamage = 999;
    private const int SuspectGain = 3;

    public YalisalinBadEnding() : base(-1, CardType.Curse, CardRarity.Ancient, TargetType.None)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Damage", DirectDamage);
            yield return new DynamicVar("SuspectGain", SuspectGain);
        }
    }

    /// <summary>
    ///     毒化一判据：该玩家的「所有牌」里是否带着坏结局（手牌 / 抽牌堆 / 弃牌堆）。
    ///     由发夹在结算火色消耗时查询。
    /// </summary>
    public static bool IsHeldBy(Player player)
    {
        return YalisalinFireComponentRules.AllCombatCards(player)
            .Any(static card => card is YalisalinBadEnding);
    }

    // 回合开始：回到手牌
    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        var source = this;
        if (player != source.Owner) return;

        if (source.Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(source, PileType.Hand);
    }

    // 回合结束：手牌每 1 张【余火】牌失去 1 点生命 + 获得嫌疑
    protected override bool HasTurnEndInHandEffectC => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext, ComponentContext componentContext)
    {
        var source = this;
        var creature = source.Owner.Creature;

        var fireCount = PileType.Hand.GetPile(source.Owner).Cards
            .Count(card => card != source && YalisalinFireComponentRules.HasFireComponent(card));

        if (fireCount > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                creature,
                fireCount,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                source,
                null);
        }

        await PowerCmd.Apply<SuspectPower>(
            choiceContext,
            creature,
            source.DynamicVars["SuspectGain"].BaseValue,
            creature,
            source,
            false);
    }

    // 被丢弃 / 被消耗：回到手牌
    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    // 打出：处刑 —— 生成免费死亡卡自动打出 → 清空自身增益 → 999 不可减免自伤
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var source = this;
        if (source.CombatState == null) return;
        var creature = source.Owner.Creature;

        var death = source.CombatState.CreateCard<Yalisalindeath>(source.Owner);
        death.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(death, PileType.Hand, source.Owner);
        await CardCmd.AutoPlay(choiceContext, death, null, skipCardPileVisuals: true);

        var buffsToRemove = creature.Powers
            .Where(power => power.Type == PowerType.Buff)
            .ToList();
        foreach (var buff in buffsToRemove)
            await PowerCmd.Remove(buff);

        await CreatureCmd.Damage(
            choiceContext,
            creature,
            source.DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            source,
            cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
