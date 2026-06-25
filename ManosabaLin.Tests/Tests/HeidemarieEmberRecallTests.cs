using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieEmberRecallTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-ember-recall");
    }

    [Fact]
    public async Task Playing_recalls_one_discard_card_and_generates_one_aurora_sword()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();
        var recalled = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([recalled]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), recalled.Pile);
        var generated = Assert.Single(AuroraSwordsIn(PileType.Hand));
        Assert.NotSame(recalled, generated);
    }

    [Fact]
    public async Task Playing_with_empty_discard_pile_does_not_generate_aurora_sword()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Empty(AuroraSwordsInCombatPiles());
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Sword_grave_card_can_be_recalled_from_discard_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();
        var swordGrave = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([swordGrave]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), swordGrave.Pile);
        Assert.Contains(swordGrave, AuroraSwordsIn(PileType.Hand));
        Assert.Equal(2, AuroraSwordsIn(PileType.Hand).Count);
    }

    [Fact]
    public async Task Upgraded_play_generates_two_aurora_swords_after_recall()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var recalled = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([recalled]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), recalled.Pile);
        Assert.Equal(2, AuroraSwordsIn(PileType.Hand).Count);
    }

    [Fact]
    public async Task Optional_selection_can_skip_a_single_discard_candidate()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();
        var candidate = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var selector = new CapturingCardSelector(Array.Empty<CardModel>());

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Contains(candidate, selector.Options);
        Assert.Same(PileType.Discard.GetPile(Player), candidate.Pile);
        Assert.Empty(AuroraSwordsInCombatPiles());
    }

    [Fact]
    public async Task Full_hand_uses_normal_overflow_without_soft_locking()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberRecall>();
        await FillHandToMax();
        var recalled = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([recalled]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), recalled.Pile);
        Assert.Empty(AuroraSwordsIn(PileType.Hand));
        var overflowedSword = Assert.Single(AuroraSwordsIn(PileType.Discard));
        Assert.Same(PileType.Discard.GetPile(Player), overflowedSword.Pile);

        await ExecuteRunnerAction();
    }

    private List<AuroraSword> AuroraSwordsIn(PileType pileType)
    {
        return pileType.GetPile(Player).Cards.OfType<AuroraSword>().ToList();
    }

    private List<AuroraSword> AuroraSwordsInCombatPiles()
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .SelectMany(pileType => pileType.GetPile(Player).Cards.OfType<AuroraSword>())
            .ToList();
    }

    private async Task FillHandToMax()
    {
        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();
    }

    private async Task<TCard> CreateCardInPile<TCard>(PileType pileType)
        where TCard : CardModel
    {
        var card = Combat.CreateCard<TCard>(Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, pileType, Player);
        await WaitFor(
            () => pileType.GetPile(Player).Cards.Contains(card),
            $"{typeof(TCard).Name} did not appear in {pileType}.");
        return card;
    }

    private async Task ClearCombatPiles()
    {
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var cards = pileType.GetPile(Player).Cards.ToArray();
            if (cards.Length > 0)
                await CardPileCmd.RemoveFromCombat(cards, skipVisuals: true);
        }

        await WaitForIdle();
    }

    private async Task ExecuteRunnerAction()
    {
        var hand = PileType.Hand.GetPile(Player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            await CardPileCmd.RemoveFromCombat(hand.Cards[0], skipVisuals: true);
            await WaitForIdle();
        }

        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
    }

    private sealed class CapturingCardSelector : ICardSelector
    {
        private readonly CardModel[] _selectedCards;

        public CapturingCardSelector(IEnumerable<CardModel> selectedCards)
        {
            _selectedCards = selectedCards.ToArray();
        }

        public bool WasCalled { get; private set; }

        public IReadOnlyList<CardModel> Options { get; private set; } = [];

        public int MinSelect { get; private set; } = -1;

        public int MaxSelect { get; private set; } = -1;

        public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
        {
            WasCalled = true;
            Options = options.ToList();
            MinSelect = minSelect;
            MaxSelect = maxSelect;
            return Task.FromResult<IEnumerable<CardModel>>(_selectedCards);
        }

        public CardRewardSelection GetSelectedCardReward(
            IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives)
        {
            throw new NotSupportedException();
        }
    }
}
