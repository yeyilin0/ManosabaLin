using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     烬火洗礼：消耗所有敌人身上的全部火色（各自从最新格开始），每消耗 1 格对全体敌人造成一段伤害；
///     一共消耗至少一整条量表（6 格）时再抽牌并获得能量。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class EmberBaptism()
    : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new CardsVar(3),
        new EnergyVar(3)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner
            || owner.Creature.CombatState is not { } combatState
            || !YalisalinFireColorSystem.TryGetHairpin(owner, out var hairpin))
            return;

        var consumed = 0;
        foreach (var enemy in combatState.Enemies.Where(static e => e.IsAlive).ToList())
            consumed += (await hairpin.ConsumeAllFireColor(choiceContext, enemy, this)).Count;

        if (consumed <= 0)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitCount(consumed)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (consumed < YalisalinsHairpin.MaxSegments)
            return;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
