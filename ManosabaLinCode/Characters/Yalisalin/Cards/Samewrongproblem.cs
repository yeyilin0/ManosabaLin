using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     同一道错题：造成两段伤害，每段攻击各自引爆 1 格；这两格颜色不同时，下一张技能牌费用变为 0。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Samewrongproblem()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const int Hits = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin);
        var logStart = hairpin?.ConsumptionLog.Count ?? 0;

        for (var i = 0; i < Hits && target.IsAlive; i++)
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);

        if (hairpin == null || hairpin.ConsumptionLog.Count - logStart < 2)
            return;

        if (hairpin.ConsumptionLog[logStart] != hairpin.ConsumptionLog[logStart + 1])
            hairpin.QueueFreeSkill();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}
