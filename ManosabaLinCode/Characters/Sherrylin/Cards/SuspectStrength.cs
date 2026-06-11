using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 嫌疑之力：使随机队友获得一点力量，次数等于友方全体嫌疑层数
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SuspectStrength() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var combatState = source.CombatState;
        if (combatState == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var allies = combatState.Allies.Where(a => a is { IsAlive: true }).ToList();
        var totalSuspect = allies.Sum(a => a.GetPower<SuspectPower>()?.Amount ?? 0);

        if (totalSuspect <= 0) return;

        var teammates = combatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c is { IsAlive: true, IsPlayer: true })
            .ToList();

        if (teammates.Count == 0) return;

        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var target = teammates[rng.NextInt(teammates.Count)];

        await PowerCmd.Apply<TempStrength>(
            choiceContext, target, totalSuspect,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
