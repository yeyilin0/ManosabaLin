using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class PerpetualFrenzy() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
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

        var gainedCards = (emotionPower.Amount / 13) - (oldAmount / 13);
        emotionPower.Amount %= 13;

        if (gainedCards > 0)
        {
            var hand = PileType.Hand.GetPile(source.Owner).Cards
                .Where(c => c != source).ToList();
            if (hand.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var target = hand[rng.NextInt(hand.Count)];
                target.SetToFreeThisTurn();
            }
        }

        // 未升级时打出移除
        if (!IsUpgraded)
            await CardPileCmd.RemoveFromCombat(source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}