using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     被缚的普罗米修斯（2 费 技能・稀有）：消耗目标 1 格火色，然后本回合你的消耗都视为同色、不触发连续，
///     每次消耗对该敌人造成伤害。升级版把本回合的颜色锁定为这张牌消耗的那一格。
/// </summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class BoundPrometheus()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BurnDamage", BoundPrometheusPower.DamagePerConsume)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BoundPrometheusPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (Owner is not { } owner || cardPlay.Target is not { } target)
            return;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        var consumed = await YalisalinFireColorSystem.ConsumeFireColor(choiceContext, owner, target, 1, this);

        var power = await PowerCmd.Apply<BoundPrometheusPower>(
            choiceContext, owner.Creature, 1, owner.Creature, this, false);
        if (power is not null && IsUpgraded && consumed.Count > 0)
            power.LockedColor = consumed[0];
    }
}
