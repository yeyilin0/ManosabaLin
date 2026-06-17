using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionHelplessnessOrb : EmotionOrb<EmotionHelplessness>
{
    protected override Color OrbColor => new(0.5f, 0.5f, 0.6f);

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        var dexDown = Owner.Creature.GetPower<DexterityPower>();
        if (dexDown != null && dexDown.Amount < 0)
        {
            var amount = -dexDown.Amount;
            await PowerCmd.Remove(dexDown);

            await PowerCmd.Apply<ThornsPower>(
                ctx, Owner.Creature, amount, Owner.Creature, null, false);

            var combatState = Owner.Creature.CombatState;
            if (combatState != null)
            {
                var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
                if (enemies.Count > 0)
                {
                    var rng = Owner.RunState.Rng.CombatCardSelection;
                    var target = enemies[rng.NextInt(enemies.Count)];
                    await PowerCmd.Apply<StrengthPower>(
                        ctx, target, -amount, Owner.Creature, null, false);
                }
            }
        }

        await OrbCmd.EvokeNext(ctx, Owner);
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}