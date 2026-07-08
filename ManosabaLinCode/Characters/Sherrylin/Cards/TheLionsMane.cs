using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheLionsMane() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(12, ValueProp.Move)
    };

  

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PlayerCmd.GainEnergy(1, Owner);

        var enemies = CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count > 0)
        {
            var rng = Owner.RunState.Rng.CombatTargets;
            var randomEnemy = rng.NextItem(enemies);

            await CreatureCmd.Damage(choiceContext, randomEnemy, DynamicVars.Damage.BaseValue, ValueProp.Move, source, cardPlay);
        }

    }

    protected override async Task AfterAutoPrePlayPhaseEntered(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        if (player != Owner) return;
        if (Pile?.Type != PileType.Exhaust) return;

        SetToFreeThisTurn();
        await CardCmd.AutoPlay(choiceContext, this, null);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
