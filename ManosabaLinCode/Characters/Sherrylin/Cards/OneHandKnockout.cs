using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class OneHandKnockout() : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BufferPower>(1m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BufferPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var combatState = source.CombatState;
        if (combatState == null) return;

        var teammates = combatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c is { IsAlive: true, IsPlayer: true })
            .ToList();

        Creature target;
        if (teammates.Count > 0)
        {
            target = teammates[0];
        }
        else
        {
            target = source.Owner.Creature;
        }

        await PowerCmd.Apply<BufferPower>(
            choiceContext, target,
            source.DynamicVars["BufferPower"].IntValue,
            source.Owner.Creature, source, false);

        await PlayerCmd.GainEnergy(
            source.DynamicVars.Energy.IntValue,
            source.Owner);

        PlayerCmd.EndTurn(target.Player, false);

        // 打出后从战斗中移除自己
        await CardPileCmd.RemoveFromCombat(source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BufferPower"].UpgradeValueBy(1m);
    }
}
