using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Sherrydeath : ManosabaCardTemplate
{
    private static readonly Type[] BasicEmotions =
    [
        typeof(EmotionJoy), typeof(EmotionSadness), typeof(EmotionAnger),
        typeof(EmotionFear), typeof(EmotionDisgust), typeof(EmotionSurprise)
    ];

    private static readonly Type[] AdvancedEmotions =
    [
        typeof(EmotionMelancholy), typeof(EmotionIrritatedFear), typeof(EmotionDesolate),
        typeof(EmotionHorrorDisgust), typeof(EmotionElation)
    ];

    public Sherrydeath() : base(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies) { }

    public override int MaxUpgradeLevel => 0;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var combatState = source.CombatState;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        // 统计自己所有牌堆中的 XlmPower 总层数
        var allPileCards = PileType.Draw.GetPile(owner).Cards
            .Concat(PileType.Hand.GetPile(owner).Cards)
            .Concat(PileType.Discard.GetPile(owner).Cards)
            .Concat(PileType.Exhaust.GetPile(owner).Cards);

        var totalXlm = 0m;
        foreach (var card in allPileCards)
        {
            // 卡牌上没有 GetPowerAmount，统计自己身上的总层数
        }

        // 直接获取自身 XlmPower 层数（所有牌的一半 = 自身的一半）
        var xlmPower = creature.GetPower<XlmPower>();
        var totalCards = allPileCards.Count();
        var xlmToGive = (int)Math.Max(1, totalCards / 2);

        // 统计案卷牌堆情绪卡数量
        var caseFilePile = owner.Piles.FirstOrDefault(p => p.Type == MainFile.CaseFilePile);
        var emotionCount = caseFilePile?.Cards.Count ?? 0;

        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);
        var rng = owner.RunState.Rng.CombatCardSelection;

        // 对全体队友生效
        var teammates = combatState.GetTeammatesOf(creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);

        foreach (var teammate in teammates)
        {
            // 消耗50层魔女化
            var withPower = teammate.GetPower<WithPower>();
            if (withPower != null && withPower.Amount > 0)
            {
                var withToRemove = Math.Min(50, (int)withPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, withPower, -withToRemove, creature, source, false);
            }

            // 消耗3层嫌疑
            var suspectPower = teammate.GetPower<SuspectPower>();
            if (suspectPower != null && suspectPower.Amount > 0)
            {
                var suspectToRemove = Math.Min(3, (int)suspectPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, suspectPower, -suspectToRemove, creature, source, false);
            }

            if (teammate.Player == null) continue;

            // ===== 给予所有牌一半数量的 XlmPower =====
            await PowerCmd.Apply<XlmPower>(
                choiceContext, teammate, xlmToGive, creature, source, false);

            // ===== 根据案卷牌堆情绪卡数量给予情绪卡 =====
            if (emotionCount >= 20)
            {
                var friendship = combatState.CreateCard<EmotionFriendship>(teammate.Player);
                await CardPileCmd.AddGeneratedCardToCombat(friendship, PileType.Draw, teammate.Player, CardPilePosition.Random);
            }

            if (emotionCount >= 4)
            {
                var chosenAdvanced = rng.NextItem(AdvancedEmotions);
                var genericMethod = createCardMethod.MakeGenericMethod(chosenAdvanced);
                var advancedCard = (CardModel)genericMethod.Invoke(combatState, [teammate.Player]);
                await CardPileCmd.AddGeneratedCardToCombat(advancedCard, PileType.Draw, teammate.Player, CardPilePosition.Random);
            }
            else if (emotionCount >= 2)
            {
                var chosenBasic = rng.NextItem(BasicEmotions);
                var genericMethod = createCardMethod.MakeGenericMethod(chosenBasic);
                var basicCard = (CardModel)genericMethod.Invoke(combatState, [teammate.Player]);
                await CardPileCmd.AddGeneratedCardToCombat(basicCard, PileType.Draw, teammate.Player, CardPilePosition.Random);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext) { }
}
