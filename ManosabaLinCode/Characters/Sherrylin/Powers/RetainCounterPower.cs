using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 蓄力反噬能力：当保留计数组件增加时对随机目标造成伤害
/// </summary>
[RegisterPower]
public sealed class RetainCounterPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        // 检查打出的卡是否有保留计数组件
        if (!cardPlay.Card.HasComponent<RetainCounterComponent>()) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        var targets = combatState.Allies.Concat(combatState.Enemies)
            .Where(c => c is { IsAlive: true } && c != Owner)
            .ToList();

        if (targets.Count == 0) return;

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var target = targets[rng.NextInt(targets.Count)];

        await CreatureCmd.Damage(choiceContext, target, Amount, ValueProp.Unpowered, Owner, null);
    }
}
