using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class LastCase() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13, DamageProps.cardUnpowered),
        new DamageVar("RepeatDamage", 4, DamageProps.cardUnpowered),
        new DamageVar("RepeatDamageFlipped", 8, DamageProps.cardUnpowered),
        new DamageVar("RepeatDamageUpgraded", 10, DamageProps.cardUnpowered)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionFusionPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFilePile = MainFile.CaseFilePile.GetPile(source.Owner);
        var caseFileCards = caseFilePile.Cards.ToList();
        if (caseFileCards.Count == 0) return;

        // 选一张保留
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, caseFileCards, source.Owner, prefs);
        var keepCard = selected.FirstOrDefault();
        if (keepCard == null) return;

        // 移除其他牌
        var toRemove = caseFileCards.Where(c => c != keepCard).ToList();
        var removeCount = toRemove.Count;

        foreach (var card in toRemove)
            await CardPileCmd.RemoveFromCombat(card);

        // 造成13点伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        // 翻案判断
        var magnifyingGlass = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        bool hasFlipped = magnifyingGlass != null && magnifyingGlass.HasTriggeredThisCombat;

        decimal repeatDamage;
        if (IsUpgraded && hasFlipped)
            repeatDamage = source.DynamicVars["RepeatDamageUpgraded"].BaseValue;
        else if (hasFlipped)
            repeatDamage = source.DynamicVars["RepeatDamageFlipped"].BaseValue;
        else
            repeatDamage = source.DynamicVars["RepeatDamage"].BaseValue;

        // 重复伤害
        for (int i = 0; i < removeCount; i++)
        {
            await DamageCmd.Attack(repeatDamage)
                .FromCard(source, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        // 翻案额外效果：生成移除数一半的基础情绪
        if (hasFlipped)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var emotionTypes = new[]
            {
                typeof(EmotionAnger), typeof(EmotionDisgust), typeof(EmotionSadness),
                typeof(EmotionFear), typeof(EmotionJoy), typeof(EmotionSurprise)
            };

            int generateCount = removeCount / 2;
            for (int i = 0; i < generateCount; i++)
            {
                var roll = rng.NextInt(emotionTypes.Length);
                var emotionCard = source.CombatState.CreateCard(
                    ModelDb.GetById<CardModel>(ModelDb.GetId(emotionTypes[roll])), source.Owner);
                if (emotionCard != null)
                    await CaseFilePileHelper.AddToCaseFilePile(
                        emotionCard, source.Owner, CardPilePosition.Top, choiceContext);
            }
        }

        // 移除 ≥8 张：触发翻案，给好奇或无助，给情绪融合
        if (removeCount >= 8)
        {
            // 触发翻案
            if (magnifyingGlass != null && !magnifyingGlass.HasTriggeredThisCombat)
            {
                var drawPile = PileType.Draw.GetPile(Owner);
                if (!drawPile.Cards.Any())
                {
                    magnifyingGlass.HasTriggeredThisCombat = true;

                    var discardPile = PileType.Discard.GetPile(Owner);
                    var exhaustPile = PileType.Exhaust.GetPile(Owner);

                    if (exhaustPile.Cards.Any())
                    {
                        var exhaustCards = exhaustPile.Cards.ToList();
                        var discardCards = discardPile.Cards.ToList();
                        magnifyingGlass.CaseReversalDiscardToExhaustCount = discardCards.Count;

                        foreach (var ec in exhaustCards)
                            await CardPileCmd.Add(ec, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
                        foreach (var dc in discardCards)
                            await CardPileCmd.Add(dc, PileType.Exhaust, CardPilePosition.Random, skipVisuals: true);
                    }
                }
            }

            // 给好奇或无助
            var rng2 = source.Owner.RunState.Rng.CombatCardSelection;
            CardModel extraCard;
            if (rng2.NextInt(2) == 0)
                extraCard = source.CombatState.CreateCard<EmotionCuriosity>(source.Owner);
            else
                extraCard = source.CombatState.CreateCard<EmotionHelplessness>(source.Owner);

            if (extraCard != null)
                await CaseFilePileHelper.AddToCaseFilePile(
                    extraCard, source.Owner, CardPilePosition.Top, choiceContext);

            // 给情绪融合
            var fusionAmount = IsUpgraded ? 2 : 1;
            await PowerCmd.Apply<EmotionFusionPower>(
                choiceContext, Owner.Creature, fusionAmount, Owner.Creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
