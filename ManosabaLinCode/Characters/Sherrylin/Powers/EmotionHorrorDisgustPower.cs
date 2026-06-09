using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 骸厌能力（厌恶+惊讶）：受到伤害时对敌方全体造成能被能力增幅的伤害并抽1，下回合开始随机对敌人造成1点伤害次数等于手牌数。
/// </summary>
[RegisterPower]
public sealed class EmotionHorrorDisgustPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _nextTurnDamageCount;

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return;
        if (dealer == null || dealer.Side == Owner.Side) return;
        if (result.TotalDamage <= 0) return;

        // 对敌方全体造成能被能力增幅的伤害
        if (Owner.CombatState != null)
        {
            foreach (var enemy in Owner.CombatState.HittableEnemies)
            {
                if (enemy.IsAlive)
                    await CreatureCmd.Damage(choiceContext, enemy, result.TotalDamage, ValueProp.Unpowered, Owner, null);
            }
        }

        // 抽1
        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);

        // 记录下回合伤害次数
        var hand = PileType.Hand.GetPile(Owner.Player);
        _nextTurnDamageCount = hand.Cards.Count;
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Side) return;

        // 下回合开始随机对敌人造成1点伤害，次数等于手牌数
        if (_nextTurnDamageCount > 0 && Owner.CombatState != null)
        {
            var enemies = new List<Creature>();
            foreach (var c in Owner.CombatState.HittableEnemies)
                if (c.IsAlive) enemies.Add(c);

            if (enemies.Count > 0)
            {
                var rng = Owner.Player.RunState.Rng.CombatCardSelection;
                var ctx = new ThrowingPlayerChoiceContext();
                for (int i = 0; i < _nextTurnDamageCount; i++)
                {
                    var enemy = rng.NextItem(enemies);
                    await CreatureCmd.Damage(ctx, enemy, 1m, ValueProp.Unpowered, Owner, null);
                }
            }
        }
        _nextTurnDamageCount = 0;
    }
}
