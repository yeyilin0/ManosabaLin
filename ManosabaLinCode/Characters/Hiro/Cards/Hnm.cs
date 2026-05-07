using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Hnm() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.AllAllies)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await PowerCmd.Apply<HnmPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}