using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 引绪取灵：随机一张额外牌组的情绪消耗，获得一张随机基础情绪，升级变为自选一张
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionDrain() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFileCards = MainFile.CaseFilePile.GetPile(source.Owner).Cards.ToList();
        if (caseFileCards.Count == 0) return;

        CardModel? toRemove = null;
        if (IsUpgraded)
        {
            var prefs = new CardSelectorPrefs(new LocString("EmotionDrain", "选择要消耗的情绪卡"), 1);
            var selection = await CardSelectCmd.FromSimpleGrid(choiceContext, caseFileCards, source.Owner, prefs);
            var selectionList = selection.ToList();
            if (selectionList.Count > 0) toRemove = selectionList[0];
        }
        else
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            toRemove = caseFileCards[rng.NextInt(caseFileCards.Count)];
        }

        if (toRemove == null) return;

        await CardPileCmd.RemoveFromCombat(toRemove);

        var rng2 = source.Owner.RunState.Rng.CombatCardSelection;
        var roll = rng2.NextInt(6);
        CardModel? emotionCard = roll switch
        {
            0 => source.CombatState.CreateCard<EmotionAnger>(source.Owner),
            1 => source.CombatState.CreateCard<EmotionDisgust>(source.Owner),
            2 => source.CombatState.CreateCard<EmotionSadness>(source.Owner),
            3 => source.CombatState.CreateCard<EmotionFear>(source.Owner),
            4 => source.CombatState.CreateCard<EmotionJoy>(source.Owner),
            5 => source.CombatState.CreateCard<EmotionSurprise>(source.Owner),
            _ => null
        };
        if (emotionCard != null)
            await CardPileCmd.Add(emotionCard, MainFile.CaseFilePile, CardPilePosition.Top);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
