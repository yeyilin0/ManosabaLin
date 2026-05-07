using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Xlm() : ManosabaCardTemplate(0, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<XlmPower>(5m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await PowerCmd.Apply<XlmPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["XlmPower"].BaseValue,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["XlmPower"].BaseValue = 10m;
    }
}