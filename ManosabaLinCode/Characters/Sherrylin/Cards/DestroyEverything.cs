using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Common.Components.Abstracts;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class DestroyEverything() : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new Common.Components.Abstracts.Sherryyuanzui()];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13, DamageProps.cardUnpowered),
        new DamageVar("PerClear", 5, DamageProps.cardUnpowered),
        new DamageVar("PerExhaust", 8, DamageProps.cardUnpowered)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source, cardPlay)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);

        // === 毁灭案件 ===
        await TriggerCaseReversal();

        int removedCount = 0;
        foreach (var pileType in new[] { PileType.Draw, PileType.Discard, PileType.Hand })
        {
            var pile = pileType.GetPile(source.Owner);
            if (pile.Cards.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var card = pile.Cards[rng.NextInt(pile.Cards.Count)];
                await CardPileCmd.RemoveFromCombat(card);
                removedCount++;
            }
        }

        for (int i = 0; i < removedCount; i++)
        {
            await PlayerCmd.GainEnergy(1m, source.Owner);
            await CardPileCmd.Draw(choiceContext, 1, source.Owner);
        }

        // === 毁灭他人 ===
        var suspectPower = Owner.Creature.GetPower<SuspectPower>();
        if (suspectPower != null)
        {
            var suspectAmount = suspectPower.Amount;
            await PowerCmd.Remove(suspectPower);

            var damage = suspectAmount * source.DynamicVars["PerClear"].IntValue;
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            {
                await CreatureCmd.Damage(choiceContext, enemy, damage, ValueProp.Unpowered, source, cardPlay);
            }
        }

        // === 毁灭自己的心 ===
        // 防御性取案卷牌堆（与 Sherrydeath 同 pattern）：非雪莉角色（例如被「魔女资料」发进牌组）可能没有该牌堆，
        // 而 PileType.GetPile 取不到会直接抛异常 ⇒ OnPlay 中途中断 ⇒ 本卡 NCard 永远留在 PlayContainer
        // （表现就是「牌卡在屏幕上打不出去」）。
        var caseFilePile = Owner.Piles.FirstOrDefault(p => p.Type == MainFile.CaseFilePile);
        var caseCards = caseFilePile?.Cards.ToList() ?? new List<CardModel>();
        var toExhaust = caseCards.OrderBy(_ => source.Owner.RunState.Rng.CombatCardSelection.NextFloat())
            .Take(4)
            .ToList();

        foreach (var card in toExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);

            // 卡面：「每失去【他人的情绪】中1张牌（最多4张）就使所有敌人受到8点伤害」
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            {
                await CreatureCmd.Damage(choiceContext, enemy, source.DynamicVars["PerExhaust"].IntValue, ValueProp.Unpowered, source, cardPlay);
            }
        }

        // === 毁灭等待 ===
        int totalCounterReset = 0;
        var allCards = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Distinct();

        foreach (var card in allCards)
        {
            if (card is IComponentsCardModel ccm)
            {
                var retainComp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
                if (retainComp != null)
                {
                    totalCounterReset += retainComp.Counter;
                    typeof(RetainCounterComponent)
                        .GetField("_counter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(retainComp, 0);
                }
            }
        }

        for (int i = 0; i < totalCounterReset; i++)
        {
            // 与「魔女资料」同类型的效果：只从图鉴可见的正牌卡里 roll，
            // 衍生卡（Token / 选项牌等 shouldShowInCardLibrary=false，如「被称为冲击波的拳风」）不参与。
            var pool = Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.EnergyCost.Canonical > 0 && c.ShouldShowInCardLibrary)
                .ToList();
            if (pool.Count > 0)
            {
                var rng = Owner.RunState.Rng.CombatCardSelection;
                var template = rng.NextItem(pool);
                var newCard = CombatState.CreateCard(template, Owner);
                newCard.EnergyCost.UpgradeBy(-1);
                    await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
            }
        }
    }

    private async Task TriggerCaseReversal()
    {
        var magnifyingGlass = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        if (magnifyingGlass == null || magnifyingGlass.HasTriggeredThisCombat) return;

        var drawPile = PileType.Draw.GetPile(Owner);
        if (drawPile.Cards.Any()) return;

        magnifyingGlass.HasTriggeredThisCombat = true;

        var discardPile = PileType.Discard.GetPile(Owner);
        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (!exhaustPile.Cards.Any()) return;

        var exhaustCards = exhaustPile.Cards.ToList();
        var discardCards = discardPile.Cards.ToList();
        magnifyingGlass.CaseReversalDiscardToExhaustCount = discardCards.Count;

        foreach (var ec in exhaustCards)
            await CardPileCmd.Add(ec, PileType.Discard, CardPilePosition.Random, skipVisuals: true);
        foreach (var dc in discardCards)
            await CardPileCmd.Add(dc, PileType.Exhaust, CardPilePosition.Random, skipVisuals: true);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
