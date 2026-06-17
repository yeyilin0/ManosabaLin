using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 冲击波的拳风：保留，给予敌人10层冲击，自己失去1点生命，攻击敌人，升级后变为零费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ShockwaveFist() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<ShockwavePower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Attack", source.Owner.Character.AttackAnimDelay);

        // 给予敌人10层冲击
        await PowerCmd.Apply<ShockwavePower>(
            choiceContext, target, 5,
            source.Owner.Creature, source, false);

        // 自己失去1点生命
        await CreatureCmd.Damage(choiceContext, source.Owner.Creature,
            1, ValueProp.Unpowered, source);

        // 攻击敌人
        await CreatureCmd.Damage(choiceContext, target,
            1, ValueProp.Move, source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}