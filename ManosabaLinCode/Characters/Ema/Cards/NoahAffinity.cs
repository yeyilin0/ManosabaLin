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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class NoahAffinity : ManosabaEmalinCardTemplate
{
    public NoahAffinity() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<NymPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(1m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var target = cardPlay.Target!;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            var enemies = CombatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
            if (enemies.Count > 0)
            {
                var rng = owner.RunState.Rng.CombatTargets;
                var randomEnemy = rng.NextItem(enemies);
                await PowerCmd.Apply<NymPower>(
                    choiceContext, randomEnemy, 1, creature, this, false);
            }
        }

        var hitCount = bond?.Affinity ?? 1;
        for (var i = 0; i < hitCount; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}