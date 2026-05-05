using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class CardEightyFivePower : ManosabaPowerTemplate
{
    private int _cardsPlayedCount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.Static(StaticHoverTip.Block);
            // 如果没有 Damage 类型的提示，可以省略或使用自定义
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var source = this;

        // 只处理持有者打出的牌
        if (cardPlay.Card.Owner.Creature != source.Owner)
            return;

        // 每打出一张牌：获得格挡
        await CreatureCmd.GainBlock(
            source.Owner,
            source.Amount,
            ValueProp.Unpowered,
            cardPlay,
            true
        );

        // 累计打出卡牌数量
        _cardsPlayedCount++;

        // 每打出两张牌：对随机敌人造成伤害
        if (_cardsPlayedCount >= 2)
        {
            _cardsPlayedCount = 0;

            var enemies = source.CombatState.HittableEnemies.ToList();
            if (enemies.Count == 0) return;

            source.Flash();

            // 对随机一个敌人造成伤害
            var target = enemies[Random.Shared.Next(enemies.Count)];

            await CreatureCmd.Damage(
                context,
                target,
                source.Amount,
                ValueProp.Unpowered,
                source.Owner,
                null
            );
        }
    }
}