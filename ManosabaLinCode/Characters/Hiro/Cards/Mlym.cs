using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Hiro;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Mlym : ManosabaCardTemplate
{
    public Mlym() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MlyPower>();
            foreach (var tip in HoverTipFactory.FromEnchantment<Mlypower>())
                yield return tip;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var owner = Owner;

        await PowerCmd.Apply<MlyPower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this, false);

        var myHand = PileType.Hand.GetPile(owner).Cards
            .Where(c => c != this)
            .ToList();
        var theirHand = PileType.Hand.GetPile(targetPlayer).Cards.ToList();

        foreach (var card in myHand)
            await CardPileCmd.RemoveFromCombat(card);
        foreach (var card in theirHand)
            await CardPileCmd.RemoveFromCombat(card);

        // 我的牌复制给队友，附魔（已有附魔则跳过）
        foreach (var card in myHand)
        {
            if (card.Enchantment != null) continue;

            var newCard = CombatState.CreateCard(card.CanonicalInstance, targetPlayer);
            if (card.CurrentUpgradeLevel > 0)
            {
                for (int i = 0; i < card.CurrentUpgradeLevel; i++)
                    CardCmd.Upgrade(newCard);
            }
            CardCmd.Enchant(ModelDb.Enchantment<Mlypower>().ToMutable(), newCard, 1m);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, targetPlayer);
        }

        // 队友的牌复制给我，附魔（已有附魔则跳过）
        foreach (var card in theirHand)
        {
            if (card.Enchantment != null) continue;

            var newCard = CombatState.CreateCard(card.CanonicalInstance, owner);
            if (card.CurrentUpgradeLevel > 0)
            {
                for (int i = 0; i < card.CurrentUpgradeLevel; i++)
                    CardCmd.Upgrade(newCard);
            }
            CardCmd.Enchant(ModelDb.Enchantment<Mlypower>().ToMutable(), newCard, 1m);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner);
        }

        await PowerCmd.Remove<MlyPower>(cardPlay.Target);
    
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}