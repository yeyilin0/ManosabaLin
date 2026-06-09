using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 瞬击：零费造成伤害，升级后添加抽一
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class InstantStrike() : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Attack", source.Owner.Character.AttackAnimDelay);
        await CreatureCmd.Damage(choiceContext, target,
            source.DynamicVars.Damage.BaseValue,
            ValueProp.Move, source);

        if (IsUpgraded)
            await CardPileCmd.Draw(choiceContext, 1, source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级添加抽一（在 OnPlay 中通过 IsUpgraded 判断）
    }
}
