using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     夹在书页里的灰：对所有敌人各给予火色；场上敌人少于 2 名时再额外给予一次。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Ashinpages()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    private const int FewEnemiesThreshold = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Fire", 2),
        new DynamicVar("ExtraFire", 2)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner || owner.Creature.CombatState is not { } combatState)
            return;

        var enemies = combatState.Enemies.Where(static e => e.IsAlive).ToList();
        foreach (var enemy in enemies)
            await YalisalinFireColorSystem.GiveFireColor(choiceContext, owner, enemy, DynamicVars["Fire"].IntValue, this);

        if (enemies.Count >= FewEnemiesThreshold)
            return;

        foreach (var enemy in enemies.Where(static e => e.IsAlive))
            await YalisalinFireColorSystem.GiveFireColor(choiceContext, owner, enemy, DynamicVars["ExtraFire"].IntValue, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
