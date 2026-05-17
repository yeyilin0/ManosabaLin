using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Ema.Powers;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class WitchChaos : ManosabaCardTemplate
{
    private static readonly Type[] EnemyDebuffTypes =
    [
        typeof(SuspectPower), typeof(WeakPower), typeof(StrengthPower), typeof(NymPower)
    ];
    private static readonly Type[] AllyBuffTypes =
    [
        typeof(MllmPower), typeof(MgmPower), typeof(XlmPower), typeof(HnmPower),
        typeof(NymPower), typeof(KkmPower), typeof(NyxmPower), typeof(AmmPower), typeof(YlsmPower),
        typeof(HiroMagicRevivePower),typeof(MlyPower)
    ];

    public WitchChaos() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Unpowered),
        new IntVar("HitCount", 13)
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var rng = source.Owner.RunState.Rng;
        var hitCount = (int)source.DynamicVars["HitCount"].BaseValue;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var enemies = source.Owner.Creature.CombatState.Creatures
            .Where(c => c.IsAlive && c.IsEnemy)
            .ToList();
        var allies = source.Owner.Creature.CombatState.Creatures
            .Where(c => c.IsAlive && !c.IsEnemy)
            .ToList();

        for (var i = 0; i < hitCount; i++)
        {
            var allTargets = enemies.Concat(allies).ToList();
            if (allTargets.Count == 0) break;

            var target = rng.Shuffle.NextItem(allTargets);

            await CreatureCmd.Damage(choiceContext, target,
                source.DynamicVars.Damage.BaseValue,
                ValueProp.Unpowered | ValueProp.Move, source);

            if (target.IsEnemy)
            {
                var powerType = rng.Shuffle.NextItem(EnemyDebuffTypes);
                await ApplyPower(choiceContext, source, target, powerType, 1m);
            }
            else
            {
                var powerType = rng.Shuffle.NextItem(AllyBuffTypes);
                await ApplyPower(choiceContext, source, target, powerType, 1m);
            }
        }
    }

    private static async Task ApplyPower(
        PlayerChoiceContext choiceContext, WitchChaos source, Creature target, System.Type powerType, decimal amount)
    {
        if (powerType == typeof(WeakPower))
            await PowerCmd.Apply<WeakPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(SuspectPower))
            await PowerCmd.Apply<SuspectPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(StrengthPower))
            await PowerCmd.Apply<StrengthPower>(choiceContext, target, -1m, source.Owner.Creature, source, false);
        else if (powerType == typeof(NymPower))
            await PowerCmd.Apply<NymPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(MllmPower))
            await PowerCmd.Apply<MllmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(MgmPower))
            await PowerCmd.Apply<MgmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(XlmPower))
            await PowerCmd.Apply<XlmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(HnmPower))
            await PowerCmd.Apply<HnmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(KkmPower))
            await PowerCmd.Apply<KkmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(NyxmPower))
            await PowerCmd.Apply<NyxmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(AmmPower))
            await PowerCmd.Apply<AmmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(YlsmPower))
            await PowerCmd.Apply<YlsmPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(HiroMagicRevivePower))
            await PowerCmd.Apply<HiroMagicRevivePower>(choiceContext, target, amount, source.Owner.Creature, source, false);
        else if (powerType == typeof(MlyPower))
            await PowerCmd.Apply<MlyPower>(choiceContext, target, amount, source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["HitCount"].UpgradeValueBy(13m); 
        EnergyCost.UpgradeBy(1);
    }
}
