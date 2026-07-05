using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 骇厌球体（厌恶+惊讶）：受到伤害时对敌方全体造成等量伤害并抽1，下回合开始随机对敌人造成1点伤害次数等于手牌数。
/// </summary>
[RegisterOrb]
public sealed class EmotionHorrorDisgustOrb : EmotionOrb<EmotionHorrorDisgust>
{
    private int _nextTurnDamageCount;

    protected override Color OrbColor => new(0.5f, 0.8f, 0.5f);

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return;
        if (dealer == null || dealer.Side == Owner.Creature.Side) return;
        if (result.TotalDamage <= 0) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState != null)
        {
            foreach (var enemy in combatState.HittableEnemies)
            {
                if (enemy.IsAlive)
                    await CreatureCmd.Damage(choiceContext, enemy, result.TotalDamage, ValueProp.Move, null, null);
            }
        }

        await CardPileCmd.Draw(choiceContext, 1, Owner);

        var hand = PileType.Hand.GetPile(Owner);
        _nextTurnDamageCount = hand.Cards.Count;
    }

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        if (_nextTurnDamageCount > 0)
        {
            var combatState = Owner.Creature.CombatState;
            if (combatState != null)
            {
                var enemies = new System.Collections.Generic.List<Creature>();
                foreach (var c in combatState.HittableEnemies)
                    if (c.IsAlive) enemies.Add(c);

                if (enemies.Count > 0)
                {
                    for (int i = 0; i < _nextTurnDamageCount; i++)
                    {
                        var enemy = enemies[i % enemies.Count];
                        await CreatureCmd.Damage(ctx, enemy, 1m, ValueProp.Move, null, null);
                    }
                }
            }
        }

        await OrbCmd.EvokeNext(ctx, Owner);
    }
}
