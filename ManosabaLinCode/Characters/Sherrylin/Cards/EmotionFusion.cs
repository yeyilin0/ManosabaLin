using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 情绪融合：1费技能牌，消耗案卷牌堆中2张基础情绪卡，生成对应的组合情绪卡。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionFusion() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var caseFileCards = MainFile.CaseFilePile.GetPile(Owner).Cards
            .Where(c => IsBaseEmotionCard(c))
            .ToList();

        if (caseFileCards.Count < 2) return;

        // 取前2张基础情绪卡
        var card1 = caseFileCards[0];
        var card2 = caseFileCards[1];

        // 确定融合结果
        var resultType = GetFusionResult(card1.GetType(), card2.GetType());
        if (resultType == null) return;

        // 消耗选中的2张卡
        await CardCmd.Exhaust(choiceContext, card1);
        await CardCmd.Exhaust(choiceContext, card2);

        // 生成组合情绪卡
        var combatState = Owner.Creature?.CombatState;
        if (combatState != null)
        {
            CardModel? fusedCard = resultType.Name switch
            {
                nameof(EmotionIrritatedFear) => combatState.CreateCard<EmotionIrritatedFear>(Owner),
                nameof(EmotionMelancholy) => combatState.CreateCard<EmotionMelancholy>(Owner),
                nameof(EmotionDesolate) => combatState.CreateCard<EmotionDesolate>(Owner),
                nameof(EmotionHorrorDisgust) => combatState.CreateCard<EmotionHorrorDisgust>(Owner),
                nameof(EmotionElation) => combatState.CreateCard<EmotionElation>(Owner),
                _ => null
            };

            if (fusedCard != null)
                await CardPileCmd.Add(fusedCard, MainFile.CaseFilePile, CardPilePosition.Top);
        }
    }

    private static Type? GetFusionResult(Type type1, Type type2)
    {
        if ((type1 == typeof(EmotionAnger) && type2 == typeof(EmotionFear)) ||
            (type1 == typeof(EmotionFear) && type2 == typeof(EmotionAnger)))
            return typeof(EmotionIrritatedFear);

        if ((type1 == typeof(EmotionJoy) && type2 == typeof(EmotionSadness)) ||
            (type1 == typeof(EmotionSadness) && type2 == typeof(EmotionJoy)))
            return typeof(EmotionMelancholy);

        if ((type1 == typeof(EmotionSadness) && type2 == typeof(EmotionFear)) ||
            (type1 == typeof(EmotionFear) && type2 == typeof(EmotionSadness)))
            return typeof(EmotionDesolate);

        if ((type1 == typeof(EmotionDisgust) && type2 == typeof(EmotionSurprise)) ||
            (type1 == typeof(EmotionSurprise) && type2 == typeof(EmotionDisgust)))
            return typeof(EmotionHorrorDisgust);

        if ((type1 == typeof(EmotionJoy) && type2 == typeof(EmotionSurprise)) ||
            (type1 == typeof(EmotionSurprise) && type2 == typeof(EmotionJoy)))
            return typeof(EmotionElation);

        return null;
    }

    private static bool IsBaseEmotionCard(CardModel card)
    {
        var type = card.GetType();
        return type == typeof(EmotionAnger) ||
               type == typeof(EmotionDisgust) ||
               type == typeof(EmotionSadness) ||
               type == typeof(EmotionFear) ||
               type == typeof(EmotionJoy) ||
               type == typeof(EmotionSurprise);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
