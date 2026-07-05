using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力横扫：带保留计数组件，攻击敌方全体，升级加数值
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainSweep() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => RetainCounterComponent.Tip;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent()];



    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Attack", source.Owner.Character.AttackAnimDelay);

        var enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
        foreach (var enemy in enemies)
        {
            await CreatureCmd.Damage(choiceContext, enemy, source.DynamicVars.Damage.BaseValue, ValueProp.Move, source, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
