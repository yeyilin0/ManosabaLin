using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Rooms;

namespace ManosabaLin.Characters.Ema.Relics;

[RegisterRelic(typeof(EmalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Emalin.Emalin))]
public sealed class EmaTrialBadge : ManosabaRelicTemplate
{
    private int _agreeCount;
    private int _doubtCount;
    private int _rebuttalCount;
    private int _lastResetRound;
    private bool _enchantedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShouldFlashOnPlayer => false;

    public int AgreeCount => _agreeCount;
    public int DoubtCount => _doubtCount;
    public int RebuttalCount => _rebuttalCount;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in HoverTipFactory.FromEnchantment<Agreement>())
                yield return tip;
            foreach (var tip in HoverTipFactory.FromEnchantment<Rebuttal>())
                yield return tip;
            foreach (var tip in HoverTipFactory.FromEnchantment<Doubt>())
                yield return tip;
        }
    }

    [SavedProperty]
    public int LastResetRound
    {
        get => _lastResetRound;
        set { AssertMutable(); _lastResetRound = value; }
    }

    [SavedProperty]
    public bool EnchantedThisCombat
    {
        get => _enchantedThisCombat;
        set { AssertMutable(); _enchantedThisCombat = value; }
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner) return;

        var combatState = player.Creature.CombatState;
        var currentRound = combatState.RoundNumber;

        if (!EnchantedThisCombat)
        {
            EnchantedThisCombat = true;
            EnchantAllTrialCards();
        }

        if (currentRound != LastResetRound)
        {
            LastResetRound = currentRound;
            _agreeCount = 0;
            _doubtCount = 0;
            _rebuttalCount = 0;
            SyncCountersToEnchantments();
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        EnchantedThisCombat = false;
        _agreeCount = 0;
        _doubtCount = 0;
        _rebuttalCount = 0;
    }

    public void IncrementCount(EnchantmentModel enchantment)
    {
        switch (enchantment)
        {
            case Agreement:
                _agreeCount++;
                break;
            case Doubt:
                _doubtCount++;
                break;
            case Rebuttal:
                _rebuttalCount++;
                break;
        }
        SyncCountersToEnchantments();
    }

    private void SyncCountersToEnchantments()
    {
        foreach (var card in PileType.Hand.GetPile(Owner).Cards)
        {
            switch (card.Enchantment)
            {
                case Agreement:
                    card.Enchantment.Amount = _agreeCount;
                    break;
                case Doubt:
                    card.Enchantment.Amount = _doubtCount;
                    break;
                case Rebuttal:
                    card.Enchantment.Amount = _rebuttalCount;
                    break;
            }
        }
    }

    private void EnchantAllTrialCards()
    {
        var allCards = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Hand.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Concat(PileType.Deck.GetPile(Owner).Cards)
            .Distinct()
            .ToList();

        foreach (var card in allCards)
        {
            if (card.Enchantment != null) continue;

            try
            {
                if (EmalinKeywordRules.HasAgreeKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Agreement>().ToMutable(), card, 1m);
                else if (EmalinKeywordRules.HasRebuttalKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Rebuttal>().ToMutable(), card, 1m);
                else if (EmalinKeywordRules.HasDoubtKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Doubt>().ToMutable(), card, 1m);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"[EmaTrialBadge] Skip {card.Id.Entry}: {ex.Message}");
            }
        }

        if (Owner?.Creature != null)
            PowerCmd.Apply<BondPower>(null, Owner.Creature, 1m, Owner.Creature, null, false);
    }
}