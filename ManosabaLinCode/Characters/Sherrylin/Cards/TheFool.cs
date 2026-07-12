using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheFool : ManosabaCardTemplate
{
    public TheFool() : base(4, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(100m),
        new PowerVar<RitualCeremonyPower>(1m),
        new PowerVar<EmotionPower>(13m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var combatState = source.CombatState;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        // 1. 获得100层魔女化
        await PowerCmd.Apply<WithPower>(choiceContext, owner.Creature, 100, owner.Creature, source, false);

        // 2. 获得1层魔女仪式
        await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, owner.Creature, 1, owner.Creature, source, false);

        // 3. 立刻进行翻案（消耗堆↔弃牌堆互换）
        var exhaustPile = PileType.Exhaust.GetPile(owner);
        var discardPile = PileType.Discard.GetPile(owner);
        var exhaustCards = exhaustPile.Cards.ToList();
        var discardCards = discardPile.Cards.ToList();

        var relic = owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        if (relic != null)
            relic.HasTriggeredThisCombat = true;

        foreach (var exhaustCard in exhaustCards)
        {
            await CardPileCmd.Add(exhaustCard, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
            await PowerCmd.Apply<XlmPower>(choiceContext, owner.Creature, 1, owner.Creature, source, false);
        }

        foreach (var discardCard in discardCards)
        {
            await CardPileCmd.Add(discardCard, PileType.Exhaust, CardPilePosition.Random, skipVisuals: true);
        }

        // 4. 获得等于抽牌堆的魔法
        var drawPileCount = PileType.Draw.GetPile(owner).Cards.Count;
        if (drawPileCount > 0)
        {
            await PowerCmd.Apply<XlmPower>(choiceContext, owner.Creature, drawPileCount, owner.Creature, source, false);
        }

        // 5. 获得13层情绪
        await PowerCmd.Apply<EmotionPower>(choiceContext, owner.Creature, 13, owner.Creature, source, false);

        // 6. 随机获得情绪卡
        if (combatState != null)
        {
            var rng = owner.RunState.Rng.CombatCardSelection;
            var baseEmotions = new Func<CardModel?>[]
            {
                () => combatState.CreateCard<EmotionAnger>(owner),
                () => combatState.CreateCard<EmotionDisgust>(owner),
                () => combatState.CreateCard<EmotionSadness>(owner),
                () => combatState.CreateCard<EmotionFear>(owner),
                () => combatState.CreateCard<EmotionJoy>(owner),
                () => combatState.CreateCard<EmotionSurprise>(owner),
            };
            var advancedEmotions = new Func<CardModel?>[]
            {
                () => combatState.CreateCard<EmotionMelancholy>(owner),
                () => combatState.CreateCard<EmotionIrritatedFear>(owner),
                () => combatState.CreateCard<EmotionDesolate>(owner),
                () => combatState.CreateCard<EmotionHorrorDisgust>(owner),
                () => combatState.CreateCard<EmotionElation>(owner),
                () => combatState.CreateCard<EmotionCuriosity>(owner),
                () => combatState.CreateCard<EmotionHelplessness>(owner),
            };

            var firstRoll = rng.NextInt(baseEmotions.Length + advancedEmotions.Length);
            bool isBase = firstRoll < baseEmotions.Length;

            CardModel? firstCard = isBase
                ? baseEmotions[firstRoll]()
                : advancedEmotions[firstRoll - baseEmotions.Length]();

            if (firstCard != null)
            {
                await CardPileCmd.AddGeneratedCardToCombat(firstCard, PileType.Discard, owner);
                await CaseFilePileHelper.AddToCaseFilePile(
                    firstCard, owner, CardPilePosition.Top, choiceContext);
            }

            CardModel? secondCard;
            if (isBase)
                secondCard = advancedEmotions[rng.NextInt(advancedEmotions.Length)]();
            else
                secondCard = baseEmotions[rng.NextInt(baseEmotions.Length)]();

            if (secondCard != null)
            {
                await CardPileCmd.AddGeneratedCardToCombat(secondCard, PileType.Discard, owner);
                await CaseFilePileHelper.AddToCaseFilePile(
                    secondCard, owner, CardPilePosition.Top, choiceContext);
            }
        }

        // 7. 按消耗堆获得护盾
        var exhaustCount = PileType.Exhaust.GetPile(owner).Cards.Count;
        if (exhaustCount > 0)
            await CreatureCmd.GainBlock(owner.Creature, exhaustCount, ValueProp.Move, cardPlay);

        // 8. 按抽牌堆回能 + 抽等量卡
        var maxEnergy = owner.PlayerCombatState.MaxEnergy;
        var currentEnergy = owner.PlayerCombatState.Energy;
        var energyToGain = Math.Min(drawPileCount, maxEnergy - currentEnergy);
        if (energyToGain > 0)
            await PlayerCmd.GainEnergy(energyToGain, owner);
        if (drawPileCount > 0)
            await CardPileCmd.Draw(choiceContext, drawPileCount, owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
