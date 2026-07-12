using MegaCrit.Sts2.Core.Commands.Builders;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Models;

namespace ManosabaLin.Characters.Common;

[RegisterSingleton]
public sealed class MultiHitVigorSingleton()
    : HookedSingletonModel(HookedSingletonModel.HookType.Combat), IAttackHitHookListener
{
    public Task BeforeAttackHit(AttackHitContext context)
    {
        if (context.HitIndex == 0) return Task.CompletedTask;
        if (context.TotalHitCount <= 1m) return Task.CompletedTask;
        if (!IsPlayerPoweredAttack(context)) return Task.CompletedTask;
        if (context.Dealer?.GetPower<VigorPower>() is not { Amount: > 0 } vigor) return Task.CompletedTask;

        context.Damage += vigor.Amount;
        return Task.CompletedTask;
    }

    private static bool IsPlayerPoweredAttack(AttackHitContext context)
    {
        if (context.Dealer?.Player is null) return false;
        if (context.Attack.Attacker != context.Dealer) return false;
        if (!context.DamageProps.IsPoweredAttack()) return false;

        return IsCardOrAnonymousAttack(context.Attack);
    }

    private static bool IsCardOrAnonymousAttack(AttackCommand attack)
    {
        return attack.ModelSource is null or CardModel;
    }
}
