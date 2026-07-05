using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Sherrylin;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Sherrybadending : ManosabaCardTemplate
{
    protected override IEnumerable<ICardComponent> CanonicalComponents => [new SherryDeath()];
    public Sherrybadending() : base(-1, CardType.Curse, CardRarity.Ancient, TargetType.None) { }

    public override int MaxUpgradeLevel => 0;



    // ===== 拦截翻案给的 XlmPower =====
    protected override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource,
        ComponentContext componentContext)
    {
        var source = this;
        if (source.Pile?.Type != PileType.Hand) return;

        if (power is XlmPower && power.Owner == source.Owner.Creature && amountChanged > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, power, -amountChanged, source.Owner.Creature, source, false);

            var allCards = new List<CardModel>();
            allCards.AddRange(PileType.Draw.GetPile(source.Owner).Cards);
            allCards.AddRange(PileType.Hand.GetPile(source.Owner).Cards.Where(c => c != this));
            allCards.AddRange(PileType.Discard.GetPile(source.Owner).Cards);

            var removeCount = allCards.Count / 2;
            var rng = source.Owner.RunState.Rng.CombatCardSelection;

            for (int i = 0; i < removeCount && allCards.Count > 0; i++)
            {
                var card = rng.NextItem(allCards);
                await CardPileCmd.RemoveFromCombat(card);
                allCards.Remove(card);
            }
        }
    }

    // ===== 回合开始：返回手牌 + 获得3层嫌疑 =====
    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        var source = this;
        if (player != source.Owner) return;

        if (source.Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(source, PileType.Hand);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, 3m,
            source.Owner.Creature, source, false);
    }

    // ===== 被丢弃返回手牌 =====
    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext, CardModel card, ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    // ===== 被消耗返回手牌 =====
    protected override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal, ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    // ===== 打出时：生成 Sherrydeath + 清除 Buff + 自伤999 =====
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        if (source.CombatState == null) return;
        var creature = source.Owner.Creature;

        // 1. 生成免费 Sherrydeath 并自动打出
        var sherryDeath = source.CombatState.CreateCard<Sherrydeath>(source.Owner);
        sherryDeath.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(sherryDeath, PileType.Hand, source.Owner);
        await CardCmd.AutoPlay(choiceContext, sherryDeath, null, skipCardPileVisuals: true);

        // 2. 清除自身全部 Buff
        var buffsToRemove = creature.Powers
            .Where(p => p.Type == PowerType.Buff)
            .ToList();
        foreach (var buff in buffsToRemove)
        {
            await PowerCmd.Remove(buff);
        }

        // 3. 对自己造成999不可减免伤害
        await CreatureCmd.Damage(choiceContext, creature, 999m, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, source, cardPlay);
    }

    // ===== 回合结束在手牌中 =====
    // ===== 回合结束在手牌中 =====
    protected override bool HasTurnEndInHandEffectC => true;

    protected override async Task OnTurnEndInHand(
        PlayerChoiceContext choiceContext, ComponentContext componentContext)
    {
        var source = this;

        // 手牌中所有蓄力组件计数 -1
        var handCards = PileType.Hand.GetPile(source.Owner).Cards;
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var counterField = typeof(RetainCounterComponent).GetField("_counter", flags);

        if (counterField != null)
        {
            foreach (var card in handCards)
            {
                if (card is IComponentsCardModel ccm)
                {
                    var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
                    if (comp != null)
                    {
                        var current = (int)counterField.GetValue(comp);
                        if (current > 1)
                            counterField.SetValue(comp, current - 1);
                    }
                }
            }
        }

        // 移除案卷牌堆中四张其他情绪卡
        var caseFilePile = source.Owner.Piles
            .FirstOrDefault(p => p.Type == MainFile.CaseFilePile);
        if (caseFilePile != null)
        {
            var otherCards = caseFilePile.Cards
                .Where(c => c != this)
                .Take(4)
                .ToList();

            foreach (var card in otherCards)
                await CardPileCmd.RemoveFromCombat(card);
        }
    }
    }
