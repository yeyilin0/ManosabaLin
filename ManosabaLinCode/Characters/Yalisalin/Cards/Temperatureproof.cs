using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     温差证明：造成伤害，给予 1 格火色并立即引爆最新的 1 格，获得格挡；引爆的是浅橙时格挡翻倍。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Temperatureproof()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8, ValueProp.Move),
        new BlockVar(5, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await YalisalinFireColorCardHelpers.Attack(choiceContext, cardPlay, this, target, DynamicVars.Damage.BaseValue);

        var consumedOrange = false;
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
        {
            await hairpin.GiveFireColor(choiceContext, target, 1, this);
            var consumed = await hairpin.ConsumeFireColor(choiceContext, target, 1, this);
            consumedOrange = consumed.Count > 0 && consumed[0] == YalisalinFireColor.LightOrange;
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        if (consumedOrange)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(2);
        DynamicVars.Block.UpgradeValueBy(3);
    }
}
