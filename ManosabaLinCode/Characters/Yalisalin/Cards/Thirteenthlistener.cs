using ManosabaLin.Characters.Yalisalin.Capabilities;
using ManosabaLin.Characters.Yalisalin.Components;
using ManosabaLin.Characters.Yalisalin.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Thirteenthlistener()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        // 每当触发火色连续，额外消耗 1 格并抽 1 张，逻辑在能力里
        await PowerCmd.Apply<ThirteenthListenerPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}
