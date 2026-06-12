using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 弃念换形：选择一张手卡本场战斗移除，随机生成一张无色消耗卡，升级改为移除两张但还是生成一张
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class AbandonReshape() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("RemoveCount", 1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var removeCount = source.DynamicVars["RemoveCount"].IntValue;

        for (int i = 0; i < removeCount; i++)
        {
            var hand = PileType.Hand.GetPile(source.Owner).Cards
                .Where(c => c != source).ToList();
            if (hand.Count == 0) break;

            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, hand, source.Owner, prefs);
            var selectedList = selected.ToList();
            if (selectedList.Count > 0)
                await CardPileCmd.RemoveFromCombat(selectedList[0]);
        }

        // 生成1张无色消耗卡
        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var allCards = ModelDb.CardPool<LinCardPool>().AllCards
            .Where(c => c.Type != CardType.Curse && c.Type != CardType.Status)
            .ToList();
        if (allCards.Count > 0)
        {
            var randomCard = allCards[rng.NextInt(allCards.Count)];
            var newCard = source.CombatState.CreateCard(randomCard, source.Owner);
            newCard.AddKeyword(CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["RemoveCount"].UpgradeValueBy(1m);
    }
}
