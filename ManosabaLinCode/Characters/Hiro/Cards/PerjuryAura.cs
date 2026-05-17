using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class PerjuryAura() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<PerjuryAuraPower>(1m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PerjuryAuraPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<PerjuryAuraPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["PerjuryAuraPower"].BaseValue,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["PerjuryAuraPower"].UpgradeValueBy(1m);
    }
}