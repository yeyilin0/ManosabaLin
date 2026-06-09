using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 妄牌逐空：将卡组顶3张牌加入手卡，本回合手卡上限加三，回合结束随机打出你当前手卡三分之一的卡，升级减一费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class MadCardChase() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<MadCardChasePower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 将牌堆顶3张加入手牌
        await CardPileCmd.Draw(choiceContext, 3, source.Owner);

        // 获得"妄牌逐空"能力（回合结束随机打出1/3手牌）
        await PowerCmd.Apply<MadCardChasePower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
