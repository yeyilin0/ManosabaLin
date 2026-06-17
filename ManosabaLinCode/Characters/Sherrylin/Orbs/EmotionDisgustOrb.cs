using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionDisgustOrb : EmotionOrb<EmotionDisgust>
{
    protected override Color OrbColor => new(0.6f, 0.8f, 0.2f);

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return;
        if (dealer == null || dealer.Side == Owner.Creature.Side) return;
        if (result.TotalDamage <= 0) return;

        await CreatureCmd.Damage(
            choiceContext, dealer, result.TotalDamage,
            ValueProp.Move, Owner.Creature, null);

        if (result.UnblockedDamage > 0)
        {
            await OrbCmd.Evoke(choiceContext, Owner, this, true);
        }
    }
}