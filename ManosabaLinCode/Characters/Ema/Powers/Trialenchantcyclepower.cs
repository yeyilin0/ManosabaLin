using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
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
    public override PowerStackType StackType => PowerStackType.Single;

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

        if (trialCards.Count == 0) return;

        // Step 2: Pick one random trial enchantment type
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

        // Step 3: Clear old enchantment and apply the new same one to all trial cards
        foreach (var (card, _) in trialCards)
        {
            CardCmd.ClearEnchantment(card);
            CardCmd.Enchant(canonical.ToMutable(), card, 1m);
        }

        // Step 4: Generate a matching keyword card, enchant it, add to hand with free this turn
        var poolCards = owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.HasModKeyword(keywordId))
            .ToList();

        if (poolCards.Count > 0)
        {
            var template = rng.NextItem(poolCards);
            var newCard = Owner.CombatState.CreateCard(template, owner);
            CardCmd.Enchant(canonical.ToMutable(), newCard, 1m);
            newCard.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner);
        }

        Flash();
    }
}
