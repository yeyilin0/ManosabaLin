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
public sealed class CardOne() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            // 未升级：有消耗；升级后：无消耗
            if (!IsUpgraded)
                yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (!IsUpgraded)
                yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }

    // 固定基础值 2
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<PerjuryPower>(2m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cardSource = this;

        // 获得 PerjuryPower
        await PowerCmd.Apply<PerjuryPower>(
            choiceContext,
            cardSource.Owner.Creature,
            cardSource.DynamicVars["PerjuryPower"].BaseValue,
            cardSource.Owner.Creature,
            cardSource,
            false
        );

        // 从弃牌堆选择一张牌返回手牌
        var prefs = new CardSelectorPrefs(cardSource.SelectionScreenPrompt, 1);
        var pile = PileType.Discard.GetPile(cardSource.Owner);
        var card = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            pile.Cards,
            cardSource.Owner,
            prefs
        )).FirstOrDefault();

        if (card != null) await CardPileCmd.Add(card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["PerjuryPower"].UpgradeValueBy(1m);
        // 失去消耗由 CanonicalKeywords 中的 IsUpgraded 判断处理
    }
}