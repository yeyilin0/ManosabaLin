using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSixtyThree() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Cards", 1m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获取手牌（排除自己）
        var handPile = PileType.Hand.GetPile(source.Owner);
        var handCards = handPile.Cards.Where(c => c != source).ToList();

        if (handCards.Count == 0) return;

        // 选择手牌，数量使用动态变量
        var selectCount = source.DynamicVars["Cards"].IntValue;
        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, selectCount, selectCount)
        {
            PretendCardsCanBePlayed = true
        };

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            handCards,
            source.Owner,
            prefs
        );

        // 给选中的卡牌添加轮回关键词
        foreach (var card in selectedCards) card.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(1m); // 升级后可选 2 张
    }
}