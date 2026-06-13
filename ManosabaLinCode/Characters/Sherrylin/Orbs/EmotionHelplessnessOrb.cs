using Godot;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 无助球体：回合结束在手卡时随机1张带蓄力组件的卡计数+1。消散时将减少敏捷消耗为零，给予随机敌人等量减力量。
/// </summary>
[RegisterOrb]
public sealed class EmotionHelplessnessOrb : EmotionOrb<EmotionHelplessness>
{
    protected override Color OrbColor => new(0.5f, 0.5f, 0.6f);

    // 回合结束：随机1张带蓄力组件的卡计数+1
    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side,
        System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (Owner.Creature?.Side != side) return;

        var retainCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c.HasComponent<RetainCounterComponent>())
            .ToList();

        if (retainCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var target = retainCards[rng.NextInt(retainCards.Count)];

        if (target is MinionLib.Component.Interfaces.IComponentsCardModel ccm)
        {
            var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (comp != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var counterField = typeof(RetainCounterComponent).GetField("_counter", flags);
                if (counterField != null)
                {
                    var current = (int)counterField.GetValue(comp);
                    counterField.SetValue(comp, current + 1);
                }
            }
        }
    }

    // 消散时：将减少敏捷消耗为零，给予随机敌人等量减力量
    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        // 读取减少敏捷的层数
        var dexDown = Owner.Creature.GetPower<TempStrengthDown>();
        if (dexDown != null && dexDown.Amount > 0)
        {
            var amount = dexDown.Amount;
            await PowerCmd.Remove(dexDown);

            // 给予随机敌人等量减力量
            var combatState = Owner.Creature.CombatState;
            if (combatState != null)
            {
                var enemies = combatState.Enemies
                    .Where(e => e is { IsAlive: true })
                    .ToList();

                if (enemies.Count > 0)
                {
                    var rng = Owner.RunState.Rng.CombatCardSelection;
                    var target = enemies[rng.NextInt(enemies.Count)];
                    await PowerCmd.Apply<StrengthPower>(
                        ctx, target, -amount, Owner.Creature, null, false);
                }
            }
        }

        await base.AfterTurnStartOrbTrigger(ctx);
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}
