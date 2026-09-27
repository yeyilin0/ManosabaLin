using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     压线提交（技能）：多段伤害，每段命中后引爆 1 格火色；本牌结算期间每次触发连续，连续奖励额外再结算一次。
///     本牌是技能牌，不走攻击牌的「攻击后消耗」，所以引爆写在牌里。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Deadlinehandoff()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5, ValueProp.Move),
        new DynamicVar("Repeats", 3)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin);
        if (hairpin != null)
            hairpin.ExtraContinuousTriggers++;

        try
        {
            for (var i = 0; i < DynamicVars["Repeats"].IntValue && target.IsAlive; i++)
            {
                await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
                if (hairpin != null && target.IsAlive)
                    await hairpin.ConsumeFireColor(choiceContext, target, 1, this);
            }
        }
        finally
        {
            if (hairpin != null)
                hairpin.ExtraContinuousTriggers--;
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Repeats"].UpgradeValueBy(1);
    }
}
