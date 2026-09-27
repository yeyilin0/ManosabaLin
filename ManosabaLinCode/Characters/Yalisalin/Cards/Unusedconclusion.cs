using ManosabaLin.Characters.Yalisalin.Powers;
using MegaCrit.Sts2.Core.Commands;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unusedconclusion()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        // 每回合第一次触发火色连续时获得能量并抽牌，逻辑在能力里
        await PowerCmd.Apply<MixedConclusionPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}
