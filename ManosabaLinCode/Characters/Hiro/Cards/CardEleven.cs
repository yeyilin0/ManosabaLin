using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardEleven() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { return new[] { CardKeyword.Exhaust }; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new CardsVar(3),
        new CardsVar("MaxExhaust", 3)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardSource = this;

        // 第一步：抽牌
        await CardPileCmd.Draw(choiceContext, cardSource.DynamicVars.Cards.BaseValue, cardSource.Owner);

        // 第二步：从手牌选择至多 3 张牌消耗
        var prefs = new CardSelectorPrefs(
            cardSource.SelectionScreenPrompt,
            0,
            (int)cardSource.DynamicVars["MaxExhaust"].BaseValue
        );

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            cardSource.Owner,
            prefs,
            null,
            cardSource
        );

        // 第三步：消耗选中的牌
        var exhaustCount = 0;
        foreach (var card in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
            exhaustCount++;
        }

        // 第四步：根据消耗的牌数获得 JusticePower 层数
        if (exhaustCount > 0)
            await PowerCmd.Apply<JusticePower>(
                choiceContext, // ★ 第一个参数
                cardSource.Owner.Creature,
                exhaustCount,
                cardSource.Owner.Creature,
                cardSource,
                false
            );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars["MaxExhaust"].UpgradeValueBy(2m);
    }
}