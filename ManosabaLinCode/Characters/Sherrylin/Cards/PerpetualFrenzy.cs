using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 永续狂潮：使当前情绪指数翻倍，如果以此提升的数值获得了情绪卡则使随机一张手卡可以免费打出一次，打出移除，升级将打出移除去掉
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class PerpetualFrenzy() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Exhaust;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var emotionPower = source.Owner.Creature.GetPower<EmotionPower>();
        if (emotionPower == null) return;

        var oldAmount = emotionPower.Amount;
        emotionPower.Amount *= 2;
        emotionPower.Flash();

        // 检查是否因此获得了情绪卡（每13层生成一张）
        var gainedCards = (emotionPower.Amount / 13) - (oldAmount / 13);
        emotionPower.Amount %= 13;

        if (gainedCards > 0)
        {
            // 随机1张手卡可以免费打出一次
            var hand = PileType.Hand.GetPile(source.Owner).Cards
                .Where(c => c != source).ToList();
            if (hand.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var target = hand[rng.NextInt(hand.Count)];
                // 给予"本回合打出免费"
                target.SetToFreeThisTurn();
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级去掉打出移除（不再有 Exhaust 关键词）
    }
}
