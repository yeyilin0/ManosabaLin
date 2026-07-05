using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class BondVerdict() : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new BlockVar(7m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
        }
    }

    private static readonly Type[] DebuffTypes =
    [
        typeof(VulnerablePower), typeof(ShrinkPower), typeof(FlankingPower),
        typeof(CrushUnderPower), typeof(ImbalancedPower), typeof(KnockdownPower),
        typeof(SmoggyPower), typeof(SurroundedPower), typeof(WeakPower),
        typeof(TagTeamPower), typeof(SlothPower), typeof(PoisonPower),
        typeof(DoomPower)
    ];

    private static readonly Type[] BuffTypes =
    [
        typeof(StrengthPower), typeof(DexterityPower), typeof(BackAttackLeftPower),
        typeof(CrabRagePower), typeof(CurlUpPower), typeof(ForbiddenGrimoirePower),
        typeof(FriendshipPower), typeof(FurnacePower), typeof(HammerTimePower),
        typeof(InfestedPower), typeof(InterceptPower), typeof(LeadershipPower),
        typeof(SneakyPower)
    ];

    public void ReduceForBondChange(int amount)
    {
        for (var i = 0; i < amount && EnergyCost.Canonical > 0; i++)
            EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond == null) return;

        if (bond.Estrangement > bond.Affinity)
        {
            var debuffCount = IsUpgraded ? bond.Estrangement : bond.Estrangement / 2;
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this, cardPlay)
                    .Targeting(enemy)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);

                for (var i = 0; i < debuffCount; i++)
                    await ApplyRandomDebuff(choiceContext, enemy);
            }
        }
        else if (bond.Affinity > bond.Estrangement)
        {
            var buffCount = IsUpgraded ? bond.Affinity : bond.Affinity / 2;
            foreach (var ally in CombatState.Creatures.Where(c => c.Side == Owner.Creature.Side && c.IsAlive))
            {
                for (var i = 0; i < buffCount; i++)
                    await ApplyRandomBuff(choiceContext, ally);
                await CreatureCmd.GainBlock(ally, DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);
            }
        }
    }

    private async Task ApplyRandomDebuff(PlayerChoiceContext choiceContext, Creature target)
    {
        var debuffType = DebuffTypes[Owner.RunState.Rng.CombatCardSelection.NextInt(DebuffTypes.Length)];
        var powerModel = (PowerModel)ModelDb.Get(debuffType).MutableClone();
        await PowerCmd.Apply(choiceContext, powerModel, target, 1m, Owner.Creature, this, false);
    }

    private async Task ApplyRandomBuff(PlayerChoiceContext choiceContext, Creature target)
    {
        var buffType = BuffTypes[Owner.RunState.Rng.CombatCardSelection.NextInt(BuffTypes.Length)];
        var powerModel = (PowerModel)ModelDb.Get(buffType).MutableClone();
        await PowerCmd.Apply(choiceContext, powerModel, target, 1m, Owner.Creature, this, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(1);
    }
}
