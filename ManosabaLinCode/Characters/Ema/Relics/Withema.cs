using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Components;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
using System.Reflection;
using System.Threading.Tasks;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Rooms;
using MinionLib.Component;

namespace ManosabaLin.Characters.Ema.Relics;

[RegisterRelic(typeof(EmalinRelicPool))]
public sealed class Withema : ManosabaRelicTemplate
{
    private int _agreeCount;
    private int _doubtCount;
    private int _rebuttalCount;
    private int _lastResetRound;
    private bool _enchantedThisCombat;
    private bool _affinity7Triggered;
    private bool _estrangement7Triggered;

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
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
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

    [SavedProperty]
    public bool Affinity7Triggered
    {
        get => _affinity7Triggered;
        set { AssertMutable(); _affinity7Triggered = value; }
    }

    [SavedProperty]
    public bool Estrangement7Triggered
    {
        get => _estrangement7Triggered;
        set { AssertMutable(); _estrangement7Triggered = value; }
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
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

        AddTrialComponentsToCards();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        EnchantedThisCombat = false;
        _agreeCount = 0;
        _doubtCount = 0;
        _rebuttalCount = 0;
        _affinity7Triggered = false;
        _estrangement7Triggered = false;
    }

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;

        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond == null) return;

        if (bond.Affinity == 7 && !_affinity7Triggered && IsAffinityCard(cardPlay.Card))
        {
            _affinity7Triggered = true;
            Flash();
            await GenerateAffinityCard();
        }

        if (bond.Estrangement == 7 && !_estrangement7Triggered && IsEstrangementCard(cardPlay.Card))
        {
            _estrangement7Triggered = true;
            Flash();
            await TransformToEstrangementCard(choiceContext);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Creature.Side) return;

        var withPower = Owner.Creature.GetPower<WithPower>();
        if (withPower != null && withPower.Amount > 0)
        {
            var toRemove = Math.Min(30, (int)withPower.Amount);
            if (toRemove > 0)
            {
                await PowerCmd.ModifyAmount(
                    new ThrowingPlayerChoiceContext(),
                    withPower,
                    -toRemove,
                    Owner.Creature,
                    (CardModel?)null,
                    false
                );
            }
        }
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
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in piles)
        {
            foreach (var card in pileType.GetPile(Owner).Cards)
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
                MainFile.Logger.Info($"[Withema] Skip {card.Id.Entry}: {ex.Message}");
            }
        }

        var componentPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
        var availableCards = componentPiles
            .SelectMany(p => p.GetPile(Owner).Cards)
            .Where(c => c.Enchantment == null && c.Rarity != CardRarity.Token && c.Type != CardType.Status && c.Type != CardType.Curse)
            .Distinct()
            .ToList();

        if (availableCards.Count >= 3)
        {
            var rng = Owner.RunState.Rng.CombatCardSelection;
            var shuffled = availableCards.OrderBy(_ => rng.NextFloat()).ToList();

            shuffled[0].TryAddComponent(new AgreementTrialComponent());
            shuffled[1].TryAddComponent(new RebuttalTrialComponent());
            shuffled[2].TryAddComponent(new DoubtTrialComponent());
        }

        if (Owner?.Creature != null)
            PowerCmd.Apply<BondPower>(null, Owner.Creature, 1m, Owner.Creature, null, false);
    }

    private void AddTrialComponentsToCards()
    {
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var componentTypes = new Func<CardComponent>[]
        {
            () => new AgreementTrialComponent(),
            () => new RebuttalTrialComponent(),
            () => new DoubtTrialComponent()
        };

        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => !c.HasComponent<AgreementTrialComponent>()
                     && !c.HasComponent<RebuttalTrialComponent>()
                     && !c.HasComponent<DoubtTrialComponent>()
                     && c.Rarity != CardRarity.Token
                     && c.Type != CardType.Status
                     && c.Type != CardType.Curse)
            .ToList();

        if (handCards.Count > 0)
        {
            var card = rng.NextItem(handCards);
            var component = rng.NextItem(componentTypes)();
            card.TryAddComponent(component);
        }

        var discardCards = PileType.Discard.GetPile(Owner).Cards
            .Where(c => !c.HasComponent<AgreementTrialComponent>()
                     && !c.HasComponent<RebuttalTrialComponent>()
                     && !c.HasComponent<DoubtTrialComponent>())
            .ToList();

        if (discardCards.Count > 0)
        {
            var card = rng.NextItem(discardCards);
            var component = rng.NextItem(componentTypes)();
            card.TryAddComponent(component);
        }
    }

    private async Task GenerateAffinityCard()
    {
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);

        var affinityTypes = new[]
        {
            typeof(SwapBodySuccess),
            typeof(GuardianOath),
            typeof(SharedFate),
            typeof(DollGift),
            typeof(TheOnlyClue),
            typeof(SubstituteCost),
            typeof(NoahAffinity),
            typeof(MargaretAffinity),
            typeof(CocoAffinity),
            typeof(AnnAffinity),
            typeof(Lyqinjin),
            typeof(BondSettlement),
        };

        var chosenType = rng.NextItem(affinityTypes);
        var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
        var newCard = (CardModel)genericMethod.Invoke(Owner.Creature.CombatState, [Owner]);
        newCard.EnergyCost.SetThisCombat(0);

        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner, CardPilePosition.Bottom);
    }

    private async Task TransformToEstrangementCard(PlayerChoiceContext choiceContext)
    {
        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, null);
        var original = selected.FirstOrDefault();
        if (original == null) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);

        var estrangementTypes = new[]
        {
            typeof(BalloonFragments),
            typeof(StabbingBlade),
            typeof(ShatteredResonance),
            typeof(WitchCleansing),
            typeof(ChainedTrust),
            typeof(PawnRealization),
            typeof(NoahEstrangement),
            typeof(MargaretEstrangement),
            typeof(CocoEstrangement),
            typeof(AnnEstrangement),
            typeof(Hiroshuyuancard),
            typeof(Lyshuyuan),
        };

        var chosenType = rng.NextItem(estrangementTypes);
        var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
        var newCard = (CardModel)genericMethod.Invoke(Owner.Creature.CombatState, [Owner]);
        newCard.EnergyCost.SetThisCombat(0);

        await CardPileCmd.RemoveFromCombat(original);
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner, CardPilePosition.Bottom);
    }

    private static bool IsAffinityCard(CardModel card)
    {
        return card is SwapBodySuccess or GuardianOath or SharedFate or DollGift
            or TheOnlyClue or SubstituteCost or NoahAffinity or MargaretAffinity
            or CocoAffinity or AnnAffinity or Lyqinjin or BondSettlement;
    }

    private static bool IsEstrangementCard(CardModel card)
    {
        return card is BalloonFragments or StabbingBlade or ShatteredResonance
            or WitchCleansing or ChainedTrust or PawnRealization or NoahEstrangement
            or MargaretEstrangement or CocoEstrangement or AnnEstrangement
            or Hiroshuyuancard or Lyshuyuan;
    }
}