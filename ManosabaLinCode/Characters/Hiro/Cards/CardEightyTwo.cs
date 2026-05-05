using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardEightyTwo() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 固定基础值 3，不再用 IsUpgraded
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Cards", 3m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 获得一层嫌疑
        await PowerCmd.Apply<SuspectPower>(choiceContext, owner.Creature, 1m, owner.Creature, source, false);

        // 选择手牌中的卡放回抽牌堆
        var returnCount = source.DynamicVars["Cards"].IntValue;
        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, returnCount);
        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            prefs,
            null,
            source
        );

        // 将选中的卡放回抽牌堆
        foreach (var card in selectedCards)
            await CardPileCmd.Add(card, (PileType)1, (CardPilePosition)1, (AbstractModel)null, false);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        // 升级：返回卡牌数 +1（3 → 4）
        DynamicVars["Cards"].BaseValue += 1;
    }
}