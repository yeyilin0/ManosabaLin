using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 狂想漫延：X卡，消耗X费获得等量计数，从抽牌堆顶持续打出卡牌并消耗等量计数，计数不足时停止。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class MadnessSpread() : ManosabaCardTemplate(-1, CardType.Attack, CardRarity.Common, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var count = source.ResolveEnergyXValue();

        var drawPile = PileType.Draw.GetPile(source.Owner);
        while (count > 0 && drawPile.Cards.Any())
        {
            var topCard = drawPile.Cards.First();
            var cost = topCard.EnergyCost.Canonical;

            // 计数不足支付这张牌的费用，立刻停止
            if (count < cost)
                break;

            count -= (int)cost;
            await CardPileCmd.AutoPlayFromDrawPile(choiceContext, source.Owner, 1, CardPilePosition.Top, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级：X+2
    }
}
