using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class WorldPurgingAurora()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    private const decimal DamagePerConsumedAuroraSword = 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var consumedAuroraSwords = 0;
        var liberatedAuroras = PileType.Hand.GetPile(Owner).Cards
            .OfType<LiberatedAurora>()
            .ToArray();

        if (liberatedAuroras.Length > 0)
        {
            foreach (var liberatedAurora in liberatedAuroras)
            {
                if (liberatedAurora.Pile?.Type == PileType.Hand)
                    await CardCmd.Exhaust(choiceContext, liberatedAurora);
            }

            consumedAuroraSwords = await AuroraSuiteHelper.ExhaustAuroraSwordsFromCombatPiles(
                choiceContext,
                Owner);
        }

        var damage = DynamicVars.Damage.BaseValue + consumedAuroraSwords * DamagePerConsumedAuroraSword;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
