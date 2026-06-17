using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class WitchificationEmotionOrb : EmotionOrb<WitchificationEmotion>
{
    protected override Color OrbColor => new(0.8f, 0f, 0.8f);

    private decimal _damageAccumulated;

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner.Creature) return 1m;
        if (target == null || target.Side == Owner.Creature.Side) return 1m;
        if (amount <= 0) return 1m;
        return 3m;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props,
        Creature target, CardModel? cardSource)
    {
        if (dealer != Owner.Creature) return;

        _damageAccumulated += result.TotalDamage;

        while (_damageAccumulated >= 20)
        {
            _damageAccumulated -= 20;

            var hpLoss = 1m;
            if (Owner.Creature.CurrentHp <= hpLoss)
                hpLoss = Owner.Creature.CurrentHp - 1;

            if (hpLoss > 0)
            {
                await CreatureCmd.Damage(choiceContext, Owner.Creature,
                    hpLoss, ValueProp.Unblockable | ValueProp.Unpowered,
                    Owner.Creature, null);
            }
        }
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        var currentHp = Owner.Creature.CurrentHp;
        if (currentHp > 0)
        {
            await CreatureCmd.Damage(ctx, Owner.Creature,
                currentHp, ValueProp.Unblockable | ValueProp.Unpowered,
                Owner.Creature, null);
        }
    }
}