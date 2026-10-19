using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

/// <summary>
///     恍然的醒悟（传火·火势库「樱花色的回想」发放）：
///     打出卡牌时随机敌人获得 3 倍层数层【魔女因子】。
/// </summary>
[RegisterPower]
public sealed class WitchFactorEnchantPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return;

        if (Owner.CombatState is not { } combatState)
            return;

        var enemy = combatState.RunState.Rng.CombatTargets.NextItem(combatState.HittableEnemies);
        if (enemy == null)
            return;

        await PowerCmd.Apply<EmaWitchFactorPower>(
            choiceContext, enemy, Amount * 3, Owner, cardPlay.Card);
    }
}
