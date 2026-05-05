using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardSeventyEight : ManosabaCardTemplate
{
    private const string GrantCountKey = "GrantCount";

    public CardSeventyEight()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new IntVar(GrantCountKey, IsUpgraded ? 2m : 1m); }
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { TransmigrationRules.TransmigrationKeywordId };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        List<CardModel> handCards = new();
        foreach (var c in PileType.Hand.GetPile(owner).Cards)
            if (c != this)
                handCards.Add(c);

        if (handCards.Count == 0)
            return;

        var grantCount = source.DynamicVars[GrantCountKey].IntValue;

        // 第一步：消耗全部手牌，每张生成带轮回的打击或防御
        foreach (var card in handCards)
        {
            await CardPileCmd.Add(card, (PileType)4, (CardPilePosition)1, null, false);

            // 随机生成打击或防御
            CardModel generated;
            if (owner.RunState.Rng.CombatCardSelection.NextBool())
                generated = CombatState.CreateCard<HiroAttack>(owner);
            else
                generated = CombatState.CreateCard<HiroDefend>(owner);

            if (IsUpgraded) CardCmd.Upgrade(generated);

            generated.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
            await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner, CardPilePosition.Bottom);
        }

        // 第二步：给牌库中的打击/防御添加轮回
        List<CardModel> drawPileCards = new();
        foreach (var c in PileType.Draw.GetPile(owner).Cards) drawPileCards.Add(c);

        var count = 0;
        foreach (var card in drawPileCards)
        {
            if (count >= 2)
                break;

            if (card is HiroAttack || card is HiroDefend)
            {
                card.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
                if (IsUpgraded) CardCmd.Upgrade(card);

                CardCmd.Preview(card);
                count++;
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 IsUpgraded 中处理
    }
}