// HiroChouma.cs - 希罗筹码
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;
using MegaCrit.Sts2.Core.Combat;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Hirochouma : ManosabaRelicTemplate, IEasyRightClickableRelic
{
    private const int CombatGoldCost = 10;
    private const int OutOfCombatGoldCost = 100;
    private const int OutOfCombatRewardChoices = 3;

    private static readonly string[] VoiceEventPaths =
    [
        "event:/ManosabaLin/sfx/relics/hiro0",
        "event:/ManosabaLin/sfx/relics/hiro1",
        "event:/ManosabaLin/sfx/relics/hiro2",
        "event:/ManosabaLin/sfx/relics/hiro3",
        "event:/ManosabaLin/sfx/relics/hiro4",
        "event:/ManosabaLin/sfx/relics/hiro5",
        "event:/ManosabaLin/sfx/relics/hiro6"
    ];

    private static readonly string[] WitchPowerCardIds =
    [
        "MANOSABA_LIN_CARD_HIRO_WITH",
        "MANOSABA_LIN_CARD_EMAWICHPOWER",
        "MANOSABA_LIN_CARD_SHERRYLIN_WITCH_POWER",
        "MANOSABA_LIN_CARD_ANANLIN_WITCH_POWER"
    ];

    private static readonly (int caseId, int weight)[] RollTable =
    [
        (0, 10),  (1, 14),  (2, 25),  (3, 20),  (4, 15),  (5, 6),  (6, 10),
    ];

    private static readonly int TotalWeight = RollTable.Sum(e => e.weight);

    private bool _triggeredThisTurn;

    public override RelicRarity Rarity => RelicRarity.Event;

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        var isInCombat = Owner.Creature.CombatState != null;
        if (isInCombat && _triggeredThisTurn) return;

        var goldCost = isInCombat ? CombatGoldCost : OutOfCombatGoldCost;
        if (Owner.Gold < goldCost) return;

        if (isInCombat) _triggeredThisTurn = true;
        Owner.Gold -= goldCost;
        Flash();

        var hiroPool = ModelDb.GetById<CardPoolModel>(ModelDb.GetId(typeof(HiroCardPool)));
        var unlockedCards = hiroPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity != CardRarity.Basic
                && c.Rarity != CardRarity.Ancient
                && c.Rarity != CardRarity.Event
                && c.CanBeGeneratedInCombat)
            .ToList();

        if (unlockedCards.Count == 0) return;

        var rng = isInCombat ? Owner.RunState.Rng.CombatCardGeneration : Owner.RunState.Rng.UpFront;
        var roll = RollWeighted(rng);
        SfxCmd.Play(VoiceEventPaths[roll]);

        CardModel generatedCard = null;
        CardModel combatCard = null;
        bool clearAllGold = false;

        switch (roll)
        {
            case 0:
                var rareZeroCost = unlockedCards
                    .Where(c => c.Rarity == CardRarity.Rare && c.EnergyCost.Canonical > 0 && !c.EnergyCost.CostsX).ToList();
                if (rareZeroCost.Count > 0)
                {
                    if (!isInCombat) { await OfferOutOfCombatCardReward(rareZeroCost, [hiroPool], c => c.Rarity == CardRarity.Rare && c.EnergyCost.Canonical > 0 && !c.EnergyCost.CostsX && c.CanBeGeneratedInCombat, rng, setCostToZero: true); return; }
                    generatedCard = CreateCardFromTemplate(rng.NextItem(rareZeroCost), isInCombat);
                    generatedCard.EnergyCost.SetThisCombat(0);
                }
                break;
            case 1:
                var rareCards = unlockedCards.Where(c => c.Rarity == CardRarity.Rare).ToList();
                if (rareCards.Count > 0)
                {
                    if (!isInCombat) { await OfferOutOfCombatCardReward(rareCards, [hiroPool], c => c.Rarity == CardRarity.Rare && c.CanBeGeneratedInCombat, rng); return; }
                    generatedCard = CreateCardFromTemplate(rng.NextItem(rareCards), isInCombat);
                }
                break;
            case 2:
                var uncommonCards = unlockedCards.Where(c => c.Rarity == CardRarity.Uncommon).ToList();
                if (uncommonCards.Count > 0)
                {
                    if (!isInCombat) { await OfferOutOfCombatCardReward(uncommonCards, [hiroPool], c => c.Rarity == CardRarity.Uncommon && c.CanBeGeneratedInCombat, rng); return; }
                    generatedCard = CreateCardFromTemplate(rng.NextItem(uncommonCards), isInCombat);
                }
                break;
            case 3:
                var commonCards = unlockedCards.Where(c => c.Rarity == CardRarity.Common).ToList();
                if (commonCards.Count > 0)
                {
                    if (!isInCombat) { await OfferOutOfCombatCardReward(commonCards, [hiroPool], c => c.Rarity == CardRarity.Common && c.CanBeGeneratedInCombat, rng); return; }
                    generatedCard = CreateCardFromTemplate(rng.NextItem(commonCards), isInCombat);
                }
                break;
            case 4: return;
            case 5:
                var deckCards = isInCombat ? Owner.Piles.SelectMany(p => p.Cards) : PileType.Deck.GetPile(Owner).Cards;
                var existingWitchCard = deckCards.FirstOrDefault(c => WitchPowerCardIds.Contains(c.Id.Entry));
                if (existingWitchCard != null)
                {
                    if (!isInCombat) { var allPools = Owner.UnlockState.CharacterCardPools.ToArray(); var allAncient = allPools.SelectMany(p => p.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)).Where(c => c.Rarity == CardRarity.Ancient).ToList(); var offered = await OfferOutOfCombatCardReward(allAncient, allPools, c => c.Rarity == CardRarity.Ancient, rng); if (offered) Owner.Gold = 0; return; }
                    generatedCard = RollAncientFromAllPools(rng, isInCombat);
                    if (isInCombat) combatCard = Owner.Creature.CombatState.CreateCard(generatedCard.CanonicalInstance, Owner);
                }
                else
                {
                    if (!isInCombat) { var allPools = Owner.UnlockState.CharacterCardPools.ToArray(); var witchCards = WitchPowerCardIds.Select(id => ModelDb.GetById<CardModel>(new ModelId("CARD", id))).Where(c => c != null).ToList(); var offered = await OfferOutOfCombatCardReward(witchCards, allPools, c => WitchPowerCardIds.Contains(c.Id.Entry), rng); if (offered) Owner.Gold = 0; return; }
                    var witchId = WitchPowerCardIds[rng.NextInt(WitchPowerCardIds.Length)];
                    var witchTemplate = ModelDb.GetById<CardModel>(new ModelId("CARD", witchId));
                    generatedCard = CreateCardFromTemplate(witchTemplate, isInCombat);
                    if (isInCombat) combatCard = Owner.Creature.CombatState.CreateCard(generatedCard.CanonicalInstance, Owner);
                }
                clearAllGold = true;
                break;
            case 6:
                var otherPools = Owner.UnlockState.CharacterCardPools.Where(p => p != Owner.Character.CardPool).SelectMany(p => p.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)).Where(c => c.Rarity != CardRarity.Ancient && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Event && c.CanBeGeneratedInCombat).ToList();
                if (otherPools.Count > 0)
                {
                    if (!isInCombat) { var rewardPools = Owner.UnlockState.CharacterCardPools.Where(p => p != Owner.Character.CardPool).ToArray(); await OfferOutOfCombatCardReward(otherPools, rewardPools, c => c.Rarity != CardRarity.Ancient && c.Rarity != CardRarity.Basic && c.Rarity != CardRarity.Event && c.CanBeGeneratedInCombat, rng); return; }
                    generatedCard = CreateCardFromTemplate(rng.NextItem(otherPools), isInCombat);
                }
                break;
        }

        if (generatedCard == null) return;
        if (clearAllGold) Owner.Gold = 0;

        if (isInCombat)
        {
            await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
            if (combatCard != null) { var deckPile = PileType.Deck.GetPile(Owner); await CardPileCmd.Add(combatCard, deckPile); }
        }
        else { var deckPile = PileType.Deck.GetPile(Owner); await CardPileCmd.Add(generatedCard, deckPile); CardCmd.Preview(generatedCard); }
    }

    private static int RollWeighted(MegaCrit.Sts2.Core.Random.Rng rng) { int roll = rng.NextInt(TotalWeight); int cumulative = 0; foreach (var (caseId, weight) in RollTable) { cumulative += weight; if (roll < cumulative) return caseId; } return RollTable[^1].caseId; }
    private CardModel CreateCardFromTemplate(CardModel template, bool isInCombat) => isInCombat ? Owner.Creature.CombatState.CreateCard(template, Owner) : Owner.RunState.CreateCard(template, Owner);

    private async Task<bool> OfferOutOfCombatCardReward(IReadOnlyCollection<CardModel> templates, IReadOnlyCollection<CardPoolModel> cardPools, Func<CardModel, bool> filter, MegaCrit.Sts2.Core.Random.Rng rng, bool setCostToZero = false)
    {
        if (templates.Count == 0 || cardPools.Count == 0) return false;
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(cardPools, filter).WithFlags(CardCreationFlags.NoRarityModification | CardCreationFlags.NoCardPoolModifications);
        var cards = templates.OrderBy(_ => rng.NextFloat()).Take(OutOfCombatRewardChoices).Select(t => Owner.RunState.CreateCard(t, Owner)).ToList();
        if (setCostToZero) { foreach (var card in cards.Where(c => !c.EnergyCost.CostsX && c.EnergyCost.Canonical > 0)) card.EnergyCost.UpgradeBy(-card.EnergyCost.Canonical); }
        if (cards.Count == 0) return false;
        var reward = new CardReward(cards, CardCreationSource.Other, Owner, options, null) { CanSkip = true, CanReroll = false };
        await RewardsCmd.OfferCustom(Owner, [reward]);
        return true;
    }

    private CardModel RollAncientFromAllPools(MegaCrit.Sts2.Core.Random.Rng rng, bool isInCombat)
    {
        var allAncient = Owner.UnlockState.CharacterCardPools.SelectMany(p => p.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)).Where(c => c.Rarity == CardRarity.Ancient).ToList();
        var template = allAncient.Count > 0 ? rng.NextItem(allAncient) : ModelDb.GetById<CardModel>(new ModelId("CARD", WitchPowerCardIds[0]));
        return isInCombat ? Owner.Creature.CombatState.CreateCard(template, Owner) : Owner.RunState.CreateCard(template, Owner);
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature)) return Task.CompletedTask;
        _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public string RightClickPrompt => "消耗金币触发效果";
}
