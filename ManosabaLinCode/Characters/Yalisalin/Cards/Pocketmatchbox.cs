using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     口袋里的火柴盒：给予 1 格火色；新填的格子与下面一格颜色不同（跨进了新的颜色段）时造成伤害。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Pocketmatchbox()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var before = hairpin.GetFireColorCount(target);
        if (await hairpin.GiveFireColor(choiceContext, target, 1, this) <= 0 || before <= 0)
            return;

        if (YalisalinsHairpin.SlotColor(before + 1) != YalisalinsHairpin.SlotColor(before) && target.IsAlive)
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(8);
    }
}
