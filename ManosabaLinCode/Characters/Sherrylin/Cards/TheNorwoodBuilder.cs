using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheNorwoodBuilder() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TheNorwoodBuilderPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Block", 3)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<TheNorwoodBuilderPower>(
            choiceContext,
            source.Owner.Creature,
            (int)source.DynamicVars["Block"].BaseValue,
            source.Owner.Creature,
            source,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Block"].UpgradeValueBy(2m);
        EnergyCost.UpgradeBy(-1);
    }
}
