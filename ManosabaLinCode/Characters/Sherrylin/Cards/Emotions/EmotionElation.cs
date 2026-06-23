using ManosabaLin.Characters.Sherrylin.Orbs;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionElation() : CaseFileCard<EmotionElationOrb>(-1, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await base.OnPlay(choiceContext, cardPlay, componentContext);

        Player player = Owner;
        int maxEnergy = player.PlayerCombatState.MaxEnergy;
        if (maxEnergy > 0)
        {
            await PlayerCmd.GainEnergy(maxEnergy, player);
            await PowerCmd.Apply<Loseengry>(
                choiceContext, player.Creature, maxEnergy,
                player.Creature, null, false);
        }
    }
}
