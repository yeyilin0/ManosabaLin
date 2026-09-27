using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>留到明天的烫伤：给予大量火色，超出量表的格数一次性补结算消耗效果（与「予燎」同规则）。</summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Tomorrowburn()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Fire", 6)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await YalisalinFireColorSystem.GiveFireColor(
            choiceContext, Owner, target, DynamicVars["Fire"].IntValue, this, overflowTriggersConsume: true);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Fire"].UpgradeValueBy(3);
    }
}
