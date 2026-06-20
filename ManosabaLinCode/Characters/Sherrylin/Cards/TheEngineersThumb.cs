using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheEngineersThumb() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("BlockMultiplier", 4),
        new DynamicVar("DrawThreshold", 2),
        new DynamicVar("DrawCount", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, source);

        var card = selected.FirstOrDefault();
        if (card == null) return;

        var cardCost = card.EnergyCost.Canonical;
        var multiplier = source.DynamicVars["BlockMultiplier"].IntValue;
        var blockAmount = cardCost * multiplier;

        await CardCmd.Exhaust(choiceContext, card);

        if (blockAmount > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, blockAmount, ValueProp.Move, cardPlay);
        }

        if (cardCost >= source.DynamicVars["DrawThreshold"].IntValue)
        {
            await CardPileCmd.Draw(choiceContext, source.DynamicVars["DrawCount"].IntValue, Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BlockMultiplier"].UpgradeValueBy(2m);
    }
}
