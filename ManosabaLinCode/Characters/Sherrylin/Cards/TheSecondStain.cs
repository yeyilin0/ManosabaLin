using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheSecondStain() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("BlockMultiplier", 5)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, discardPile.Cards, Owner, prefs);
        var selectedCard = selected.FirstOrDefault();
        if (selectedCard == null) return;

        var cardCost = selectedCard.EnergyCost.Canonical;

        // 从弃牌堆移到手牌
        await CardPileCmd.Add(selectedCard, PileType.Hand);

        // 免费打出
        selectedCard.SetToFreeThisTurn();
        await CardCmd.AutoPlay(choiceContext, selectedCard, null);

        // 打出后消耗
        await CardPileCmd.RemoveFromCombat(selectedCard);

        if (IsUpgraded && cardCost > 0)
        {
            var blockAmount = cardCost * DynamicVars["BlockMultiplier"].IntValue;
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BlockMultiplier"].UpgradeValueBy(3m);
    }
}
