using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionMimic() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [RemoveOnPlayComponent.Tip];

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RemoveOnPlayComponent()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFileCards = MainFile.CaseFilePile.GetPile(source.Owner).Cards
            .Where(c => IsBaseEmotion(c))
            .ToList();

        if (caseFileCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, caseFileCards, source.Owner, prefs);
        var card = selected.FirstOrDefault();
        if (card != null)
        {
            var newCard = source.CombatState.CreateCard(card.CanonicalInstance, source.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, source.Owner);
        }
    }

    private static bool IsBaseEmotion(CardModel card)
    {
        return card is EmotionAnger or EmotionDisgust or EmotionSadness
            or EmotionFear or EmotionJoy or EmotionSurprise;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
