using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class AbandonReshape() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Common, TargetType.Self)
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

        // 从无色卡池随机生成1张带消耗的卡
        // 从无色卡池随机生成1张带消耗的卡
        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var colorlessExhaustCards = ModelDb.CardPool<ColorlessCardPool>().AllCards
            .Where(c => c.Rarity != CardRarity.Token
                        && c.Type != CardType.Curse
                        && c.Type != CardType.Status
                        && c.Keywords.Contains(CardKeyword.Exhaust))
            .ToList();

        if (colorlessExhaustCards.Count > 0)
        {
            var randomCard = colorlessExhaustCards[rng.NextInt(colorlessExhaustCards.Count)];
            var newCard = source.CombatState.CreateCard(randomCard, source.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }

        if (colorlessExhaustCards.Count > 0)
        {
            var randomCard = colorlessExhaustCards[rng.NextInt(colorlessExhaustCards.Count)];
            var newCard = source.CombatState.CreateCard(randomCard, source.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["RemoveCount"].UpgradeValueBy(1m);
    }
}
