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
/// 被称为冲击波的拳风：保留，获得10层冲击（攻击敌人时额外使其失去等量生命），自己失去1点生命，随机攻击敌人，升级后变为零费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class ShockwaveFist() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.RandomEnemy, false)
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

        // 获得10层冲击
        await PowerCmd.Apply<ShockwavePower>(
            choiceContext, source.Owner.Creature, 10,
            source.Owner.Creature, source, false);

        // 自己失去1点生命
        await CreatureCmd.Damage(choiceContext, source.Owner.Creature,
            1, ValueProp.Unpowered, source);

        // 随机攻击敌人
        await CreatureCmd.Damage(choiceContext, target,
            0, ValueProp.Move, source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
