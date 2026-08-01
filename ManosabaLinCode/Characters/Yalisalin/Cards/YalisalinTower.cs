using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class YalisalinTower()
    : ManosabaCardTemplate(0, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}
