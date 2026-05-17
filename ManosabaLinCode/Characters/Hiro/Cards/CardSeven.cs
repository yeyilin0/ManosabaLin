using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardSeven() : ManosabaCardTemplate(0, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override bool GainsBlock => true;

    private int RequiredJusticeAmount => IsUpgraded ? 4 : 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RitualCeremonyPower>(1m),
        new BlockVar(10m, ValueProp.Move),
        new PowerVar<JusticePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC) return false;
            var justicePower = Owner.Creature.GetPower<JusticePower>();
            return (justicePower?.Amount ?? 0) >= RequiredJusticeAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);

        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        if (justicePower != null && justicePower.Amount >= RequiredJusticeAmount)
            await PowerCmd.ModifyAmount(
                choiceContext, justicePower,
                -RequiredJusticeAmount,
                source.Owner.Creature,
                source,
                false
            );

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(5m);
    }
}
