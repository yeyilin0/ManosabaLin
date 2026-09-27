using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Twodifferenttestimonies()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move),
        new DynamicVar("Hits", 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        YalisalinFireColor? previousColor = null;
        var hits = IsUpgraded ? 3 : 2;
        DynamicVars["Hits"].BaseValue = hits;

        YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin);

        for (var i = 0; i < hits; i++)
        {
            // 每段伤害后的消耗由发夹的「攻击后消耗火色」完成，这里只读消耗记录判断颜色。
            var logStart = hairpin?.ConsumptionLog.Count ?? 0;
            await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);
            if (hairpin == null || hairpin.ConsumptionLog.Count <= logStart)
                continue;

            var currentColor = hairpin.ConsumptionLog[logStart];

            if (previousColor != null && previousColor.Value != currentColor)
                await YalisalinFireColorSystem.ResolveExtraFireColorReward(choiceContext, Owner, currentColor, this);

            previousColor = currentColor;
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Hits"].UpgradeValueBy(1);
    }
}
