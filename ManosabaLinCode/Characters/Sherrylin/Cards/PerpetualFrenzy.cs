using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class PerpetualFrenzy() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionPower>();
            yield return RemoveOnPlayComponent.Tip;
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

        // 计算因翻倍而额外生成的情绪卡数量
        var gainedCards = (emotionPower.Amount / 13) - (oldAmount / 13);
        emotionPower.Amount %= 13;

        // 手动生成情绪卡
        if (gainedCards > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var combatState = source.CombatState;

            for (int i = 0; i < gainedCards; i++)
            {
                var roll = rng.NextInt(11);
                CardModel? emotionCard = roll switch
                {
                    0 => combatState.CreateCard<EmotionAnger>(source.Owner),
                    1 => combatState.CreateCard<EmotionDisgust>(source.Owner),
                    2 => combatState.CreateCard<EmotionSadness>(source.Owner),
                    3 => combatState.CreateCard<EmotionFear>(source.Owner),
                    4 => combatState.CreateCard<EmotionJoy>(source.Owner),
                    5 => combatState.CreateCard<EmotionSurprise>(source.Owner),
                    6 => combatState.CreateCard<EmotionMelancholy>(source.Owner),
                    7 => combatState.CreateCard<EmotionIrritatedFear>(source.Owner),
                    8 => combatState.CreateCard<EmotionDesolate>(source.Owner),
                    9 => combatState.CreateCard<EmotionHorrorDisgust>(source.Owner),
                    10 => combatState.CreateCard<EmotionElation>(source.Owner),
                    _ => null
                };

                if (emotionCard != null)
                    await CardPileCmd.Add(emotionCard, MainFile.CaseFilePile, CardPilePosition.Top);
            }

            // 随机1张手卡本回合费用变为0
            var hand = PileType.Hand.GetPile(source.Owner).Cards
                .Where(c => c != source).ToList();
            if (hand.Count > 0)
            {
                var target = hand[rng.NextInt(hand.Count)];
                target.SetToFreeThisTurn();
            }
        }

        // 未升级时打出移除
        if (!IsUpgraded)
            source.TryAddComponent(new RemoveOnPlayComponent());
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
