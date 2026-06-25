using System.Runtime.CompilerServices;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class AuroraSword()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false), IAuroraSwordCard
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new SwordGraveComponent(),
        new LinkComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target == null)
            return;

        await DealDamage(choiceContext, cardPlay.Target);
    }

    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this) || Pile?.Type != PileType.Discard)
            return;

        var target = RandomHittableEnemy();
        if (target == null)
            return;

        await DealDamage(choiceContext, target);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }

    private Creature? RandomHittableEnemy()
    {
        var combatState = CombatState ?? Owner.Creature.CombatState;
        return Owner.RunState.Rng.CombatTargets.NextItem(combatState?.HittableEnemies ?? []);
    }

    private async Task DealDamage(PlayerChoiceContext choiceContext, Creature target)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }
}

internal static class AuroraSwordTokenFactoryRegistration
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register()
    {
        SwordTokenGeneration.RegisterTokenCard<AuroraSword>(SwordTokenKind.Aurora);
    }
}
