using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>
///     火种（0 费 攻击・衍生）：
///     由「火种盒」生成，造成 5 点伤害并给予目标 1 格火色，打出后从本场移除。
/// </summary>
[RegisterCard(typeof(TokenCardPool))]
public sealed class KindlingSparkToken()
    : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await ResolveKindling(choiceContext, cardPlay);

        // 「打出移除」：与「子弹」同款，直接在 OnPlay 里下移除命令，不挂组件。
        await CardPileCmd.RemoveFromCombat(this);
    }

    private async Task ResolveKindling(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { } owner || cardPlay.Target is not { } target)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 攻击本身已按发夹规则引爆 1 格；之后再给予 1 格。
        if (target.IsAlive)
            await YalisalinFireColorSystem.GiveFireColor(choiceContext, owner, target, 1, this);
    }
}
