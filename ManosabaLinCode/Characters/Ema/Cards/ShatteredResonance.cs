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
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Common.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class ShatteredResonance : ManosabaEmalinCardTemplate
{
    public ShatteredResonance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<HnmPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("StrGain", 4)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        await PowerCmd.Apply<HnmPower>(
            choiceContext, creature, 1, creature, this, false);

        var strGain = DynamicVars["StrGain"].BaseValue;

        foreach (var enemy in CombatState.Enemies.Where(e => e is { IsAlive: true }))
        {
            await PowerCmd.Apply<TempStrength>(
                choiceContext, enemy, strGain, creature, this, false);
        }

        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e is { IsAlive: true }))
            {
                await PowerCmd.Apply<CrushUnderPower>(
                    choiceContext, enemy, -10, creature, this, false);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrGain"].UpgradeValueBy(2);
    }
}