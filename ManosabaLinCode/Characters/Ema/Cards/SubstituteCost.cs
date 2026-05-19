using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
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
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SubstituteCost : ManosabaCardTemplate
{
    public SubstituteCost() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("GainEnergy", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        var affinity = bond?.Affinity ?? 0;
        if (affinity > 0)
        {
            await PowerCmd.Apply<TempStrength>(
                choiceContext, creature, affinity, creature, this, false);
        }

        await PowerCmd.Apply<LoseEnergyPower>(
            choiceContext, creature, 2, creature, this, false);

        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            await PowerCmd.Apply<GainEnergyPower>(
                choiceContext, creature, DynamicVars["GainEnergy"].BaseValue, creature, this, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["GainEnergy"].UpgradeValueBy(1);
    }
}
