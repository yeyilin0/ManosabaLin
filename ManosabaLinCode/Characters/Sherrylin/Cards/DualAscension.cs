using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class DualAscension() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DualAscensionPower>(5m)
    ];

  

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        if (IsUpgraded)
        {
            await PowerCmd.Apply<DualAscension2Power>(
                choiceContext, source.Owner.Creature,
                source.DynamicVars["DualAscensionPower"].BaseValue,
                source.Owner.Creature, source, false);
        }
        else
        {
            await PowerCmd.Apply<DualAscensionPower>(
                choiceContext, source.Owner.Creature,
                source.DynamicVars["DualAscensionPower"].BaseValue,
                source.Owner.Creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
