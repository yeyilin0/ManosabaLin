using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 情绪摹写：复制额外牌组里面的一张基础情绪，升级减一费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionMimic() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
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
        var selectedList = selected.ToList();
        if (selectedList.Count > 0)
        {
            var newCard = source.CombatState.CreateCard(selectedList[0].CanonicalInstance, source.Owner);
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
