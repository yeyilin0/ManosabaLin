using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Emalin.Components;
using MegaCrit.Sts2.Core.Entities.Players;
using MinionLib.Component.Interfaces;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmaForgottenOne : ManosabaCardTemplate
{
    protected override IEnumerable<ICardComponent> CanonicalComponents => [new EmaDeath()];
    private const int DirectDamage = 999;
    private const int WithPowerGain = 20;
    private const int SuspectGain = 3;

    public EmaForgottenOne() : base(-1, CardType.Curse, CardRarity.Ancient, TargetType.None)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Damage", DirectDamage);
            yield return new DynamicVar("WithGain", WithPowerGain);
            yield return new DynamicVar("SuspectGain", SuspectGain);
        }
    }

    // 回合开始：回到手牌 + 羁绊毒化
    protected override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        var source = this;
        if (player != source.Owner) return;

        // 如果不在手牌，回到手牌
        if (source.Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(source, PileType.Hand);

        var bond = source.Owner.Creature.GetPower<BondPower>();
        if (bond == null) return;

        var minVal = Math.Min(bond.Affinity, bond.Estrangement);
        bond.Affinity = minVal;
        bond.Estrangement = minVal;
    }

    // 打出攻击牌时：魔女化毒化 - 获得20层魔女化
    protected override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        if (source.Pile?.Type != PileType.Hand) return;
        if (cardPlay.Card.Owner != source.Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        await PowerCmd.Apply<WithPower>(
            context,
            source.Owner.Creature,
            source.DynamicVars["WithGain"].BaseValue,
            source.Owner.Creature,
            cardPlay.Card,
            false
        );
    }

    // 回合结束：审判毒化 + 嫌疑毒化
    protected override bool HasTurnEndInHandEffectC => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;

        // 审判毒化：计算审判附魔数量
        var trialCount = CountTrialEnchantments(owner);

        // 失去等于审判附魔数量的HP
        if (trialCount > 0)
        {
            await CreatureCmd.Damage(choiceContext, creature, trialCount,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, source);
        }

        // 移除四分之一的审判附魔
        var toRemove = trialCount / 4;
        if (toRemove > 0)
        {
            await RemoveTrialEnchantments(owner, toRemove);
        }

        // 嫌疑毒化：获得3层嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext,
            creature,
            source.DynamicVars["SuspectGain"].BaseValue,
            creature,
            source,
            false
        );
    }

    private static int CountTrialEnchantments(Player owner)
    {
        var count = 0;
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(owner);
            foreach (var card in pile.Cards)
            {
                if (card.Enchantment is Rebuttal or Agreement or Doubt)
                    count++;
            }
        }
        return count;
    }

    private static async Task RemoveTrialEnchantments(Player owner, int count)
    {
        var rng = owner.RunState.Rng.CombatCardSelection;
        var trialCards = new List<CardModel>();
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(owner);
            foreach (var card in pile.Cards)
            {
                if (card.Enchantment is Rebuttal or Agreement or Doubt)
                    trialCards.Add(card);
            }
        }

        var toRemove = trialCards.OrderBy(_ => rng.NextFloat()).Take(count).ToList();
        foreach (var card in toRemove)
        {
            var template = card.CanonicalInstance;
            var upgradeLevel = card.CurrentUpgradeLevel;
            var pileType = card.Pile?.Type ?? PileType.Deck;
            await CardPileCmd.RemoveFromCombat(card);
            var newCard = owner.Creature.CombatState.CreateCard(template, owner);
            for (int i = 0; i < upgradeLevel; i++)
                CardCmd.Upgrade(newCard);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, pileType, owner);
        }
    }

    // 打出时：生成Emadeath并自动打出，然后造成999点伤害
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        if (source.CombatState == null) return;
        var creature = source.Owner.Creature;

        var emaDeath = source.CombatState.CreateCard<Emadeath>(source.Owner);
        emaDeath.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(emaDeath, PileType.Hand, source.Owner);
        await CardCmd.AutoPlay(choiceContext, emaDeath, null, skipCardPileVisuals: true);

        var buffsToRemove = creature.Powers
            .Where(p => p.Type == PowerType.Buff)
            .ToList();
        foreach (var buff in buffsToRemove)
        {
            await PowerCmd.Remove(buff);
        }

        await CreatureCmd.Damage(choiceContext, creature, source.DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, source);
    }

    // 被丢弃时返回手牌
    protected override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card, ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    // 被消耗时返回手牌
    protected override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal, ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}