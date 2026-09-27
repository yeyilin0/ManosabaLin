using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     放学后的试烧：造成伤害（攻击本身会引爆 1 格），再消耗 1 格；
///     这两格若凑成同色连续，追加一段伤害。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Afterschooltestburn()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6, ValueProp.Move),
        new DynamicVar("ExtraDamage", 4)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
        {
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
            return;
        }

        var continuousBefore = hairpin.ContinuousTriggersThisCombat;
        await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
        await hairpin.ConsumeFireColor(choiceContext, target, 1, this);

        if (hairpin.ContinuousTriggersThisCombat > continuousBefore && target.IsAlive)
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars["ExtraDamage"].BaseValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3);
        DynamicVars["ExtraDamage"].UpgradeValueBy(2);
    }
}
