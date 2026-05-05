using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSixteen() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var drawCount = 0;

        // 抽牌直到抽到攻击牌，或手牌满 10 张
        while (true)
        {
            var drawnCard = await CardPileCmd.Draw(choiceContext, source.Owner);

            if (drawnCard == null)
                break;

            drawCount++;

            // 如果抽到攻击牌，停止抽牌
            if (drawnCard.Type == CardType.Attack)
                break;

            // 手牌满 10 张也停止
            if (PileType.Hand.GetPile(source.Owner).Cards.Count >= 10)
                break;
        }

        // 获得等于抽牌数的伪证层数
        if (drawCount > 0)
            await PowerCmd.Apply<PerjuryPower>(
                choiceContext, source.Owner.Creature,
                drawCount,
                source.Owner.Creature,
                source,
                false
            );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 费用 1 → 0
    }
}