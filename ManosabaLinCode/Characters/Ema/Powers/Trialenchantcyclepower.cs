using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Trialenchantcyclepower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private static readonly Type[] EnchantTypes = [typeof(Rebuttal), typeof(Agreement), typeof(Doubt)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        var owner = Owner.Player;
        var rng = owner.RunState.Rng.CombatCardSelection;

        // Step 1: Find all trial-enchanted cards across piles
        var allPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
        var trialCards = new List<(CardModel card, PileType pile)>();

        foreach (var pileType in allPiles)
        {
            var pile = pileType.GetPile(owner);
            foreach (var card in pile.Cards)
            {
                if (card.Enchantment is Rebuttal or Agreement or Doubt)
                    trialCards.Add((card, pileType));
            }
        }

        var removedCount = trialCards.Count;
        if (removedCount == 0) return;

        // Step 2: Remove and recreate each card without enchantment
        foreach (var (card, pileType) in trialCards)
        {
            var template = card.CanonicalInstance;
            var upgradeLevel = card.CurrentUpgradeLevel;
            await CardPileCmd.RemoveFromCombat(card);
            var newCard = Owner.CombatState.CreateCard(template, owner);
            for (int i = 0; i < upgradeLevel; i++)
                CardCmd.Upgrade(newCard);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, pileType, owner);
        }

        // Step 3: Pick one random trial enchantment type
        var chosenType = rng.NextItem(EnchantTypes);
        EnchantmentModel canonical;
        string keywordId;

        if (chosenType == typeof(Rebuttal))
        {
            canonical = ModelDb.Enchantment<Rebuttal>();
            keywordId = EmalinKeywordRules.RebuttalKeywordId;
        }
        else if (chosenType == typeof(Agreement))
        {
            canonical = ModelDb.Enchantment<Agreement>();
            keywordId = EmalinKeywordRules.AgreeKeywordId;
        }
        else
        {
            canonical = ModelDb.Enchantment<Doubt>();
            keywordId = EmalinKeywordRules.DoubtKeywordId;
        }

        // Step 4: Enchant random unenchanted cards in deck
        var unenchanted = PileType.Deck.GetPile(owner).Cards
            .Where(c => c.Enchantment == null)
            .ToList();

        var toEnchant = unenchanted
            .OrderBy(_ => rng.NextFloat())
            .Take(removedCount)
            .ToList();

        foreach (var card in toEnchant)
        {
            CardCmd.Enchant(canonical.ToMutable(), card, 1m);
        }

        // Step 5: Generate a card with matching keyword, enchant it, add to hand
        var generatedCards = CardFactory.GetDistinctForCombat(
            owner,
            owner.Character.CardPool
                .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.HasModKeyword(keywordId)),
            1,
            owner.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (generatedCards.Count > 0)
        {
            var genCard = generatedCards[0];
            if (genCard.Enchantment == null)
                CardCmd.Enchant(canonical.ToMutable(), genCard, 1m);
            await CardPileCmd.AddGeneratedCardToCombat(genCard, PileType.Hand, owner);
        }

        Flash();
    }
}
