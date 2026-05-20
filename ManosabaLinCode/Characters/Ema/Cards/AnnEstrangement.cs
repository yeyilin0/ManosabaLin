using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class AnnEstrangement : ManosabaCardTemplate
{
    public AnnEstrangement() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("DamageMult", 2)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var target = cardPlay.Target!;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        await PowerCmd.Apply<WeakPower>(
            choiceContext, target, 2, creature, this, false);

        await PowerCmd.Apply<FrailPower>(
            choiceContext, target, 2, creature, this, false);

        if (bond != null && bond.Estrangement >= bond.Affinity)
        {
            var multiplier = DynamicVars["DamageMult"].IntValue;
            var damage = bond.Estrangement * multiplier;
            if (damage > 0)
            {
                await CreatureCmd.Damage(choiceContext, target, damage,
                    ValueProp.Unpowered | ValueProp.Move, creature, this);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["DamageMult"].UpgradeValueBy(1);
    }
}
