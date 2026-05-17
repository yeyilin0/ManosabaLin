using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class GuardianOath : ManosabaEmalinCardTemplate
{
    public GuardianOath() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<YlsmPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("AmmStacks", 3),
        new EnergyVar(1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        var allTargets = CombatState.Allies.Concat(CombatState.Enemies)
            .Where(c => c is { IsAlive: true })
            .ToList();
        if (allTargets.Count > 0)
        {
            var rng = owner.RunState.Rng.CombatTargets;
            var target = rng.NextItem(allTargets);

            // 分三次，每次施加1层
            for (int i = 0; i < 3; i++)
            {
                await PowerCmd.Apply<YlsmPower>(
                    choiceContext, target, 1, creature, this, false);
            }

            if (target.Side == creature.Side && target != creature)
            {
                if (target.Player != null)
                    await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, target.Player);

                if (bond != null && bond.Affinity > bond.Estrangement)
                    await CreatureCmd.Heal(target, 1m);
            }
            else if (target.Side != creature.Side)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext, target, 1, creature, this, false);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}