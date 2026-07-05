using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class MadnessSpread() : ManosabaCardTemplate(-1, CardType.Attack, CardRarity.Common, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var count = source.ResolveEnergyXValue();
        var rng = source.Owner.RunState.Rng.CombatCardSelection;

        var drawPile = PileType.Draw.GetPile(source.Owner);
        while (count > 0 && drawPile.Cards.Any())
        {
            var topCard = drawPile.Cards.First();
            var cost = topCard.EnergyCost.Canonical;

            if (count < cost)
                break;

            count -= (int)cost;
            await CardPileCmd.AutoPlayFromDrawPile(choiceContext, source.Owner, 1, CardPilePosition.Top, false);

            // 每打出一张卡牌对随机敌人造成1点伤害
            var enemies = source.CombatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count > 0)
            {
                var target = enemies[rng.NextInt(enemies.Count)];
                await CreatureCmd.Damage(choiceContext, target, 1, ValueProp.Unpowered, source, cardPlay);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // X+2
    }
}
