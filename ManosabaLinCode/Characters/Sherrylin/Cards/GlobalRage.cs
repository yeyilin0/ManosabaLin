using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 全域怒意：对敌方全体造成伤害，每打到一个加一层情绪，升级加伤害
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class GlobalRage() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var damage = source.DynamicVars.Damage.BaseValue;

        var enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();

        foreach (var enemy in enemies)
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                damage,
                ValueProp.Move,
                source.Owner.Creature,
                source);

            await PowerCmd.Apply<EmotionPower>(
                choiceContext, source.Owner.Creature, 1,
                source.Owner.Creature, null, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
