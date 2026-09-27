using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     反向验算：获得格挡，消耗目标本回合被给予的最早一格火色（有意不从最新格开始），该次消耗效果翻倍。
///     本回合没给予过火色的目标不会被消耗。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Reversecalculation()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    private const int EffectMultiplier = 2;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5, ValueProp.Move),
        new DynamicVar("Consume", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        for (var i = 0; i < DynamicVars["Consume"].IntValue; i++)
        {
            if (await hairpin.ConsumeEarliestGivenThisTurn(choiceContext, target, this, EffectMultiplier) == null)
                break;
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Consume"].UpgradeValueBy(1);
    }
}
