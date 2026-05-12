using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Dismantledparts : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

   

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new EnergyVar(1); }
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player != Owner ? amount : amount + DynamicVars.Energy.BaseValue;
    }

    public override async Task AfterAutoPrePlayPhaseEnteredLate(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner) return;
        var combatState = player.Creature.CombatState;
        if (combatState.RoundNumber > 1) return;

        Flash();

        var enemies = combatState.HittableEnemies
            .Where(c => c != null && c.IsAlive)
            .ToList();
        if (enemies.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatTargets;

        int cardsPlayed = 0;
        while (cardsPlayed < 13 &&
               !CombatManager.Instance.IsOverOrEnding &&
               !CombatManager.Instance.IsPlayerReadyToEndTurn(player))
        {
            var card = PileType.Hand.GetPile(Owner).Cards
                .FirstOrDefault(c => c.CanPlay() && c.Type == CardType.Attack);
            if (card == null) break;

            var target = rng.NextItem(enemies);
            await card.SpendResources();
            await CardCmd.AutoPlay(choiceContext, card, target, skipXCapture: true);
            cardsPlayed++;
        }
    }
}
