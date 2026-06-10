using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 怪力：雪莉的专属魔法卡，回合开始获得20魔女化，获得一张被称为冲击波的拳风，升级获得固有并且获得称为冲击波的拳风的升级版
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SuperStrength() : ManosabaCardTemplate(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
   

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuperStrengthPower>();
            yield return HoverTipFactory.FromCard<ShockwaveFist>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var power = await PowerCmd.Apply<SuperStrengthPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);
        if (power is SuperStrengthPower superPower)
            superPower.CardUpgraded = IsUpgraded;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}
