using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieLingeringAuroraLinkTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-lingering-aurora-link");
    }

    [Fact]
    public async Task Playing_can_take_card_from_draw_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();
        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([drawCard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), drawCard.Pile);
    }

    [Fact]
    public async Task Playing_can_take_card_from_discard_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();
        var discardCard = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discardCard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), discardCard.Pile);
    }

    [Fact]
    public async Task Draw_and_discard_candidates_share_one_selection_grid()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();
        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var discardCard = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var selector = new CapturingCardSelector([discardCard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(1, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Contains(drawCard, selector.Options);
        Assert.Contains(discardCard, selector.Options);
        Assert.Same(PileType.Draw.GetPile(Player), drawCard.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), discardCard.Pile);
    }

    [Fact]
    public async Task Selected_card_gains_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();
        var selected = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([selected]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(((CardModel)selected).HasComponent<LinkComponent>());
    }

    [Fact]
    public async Task No_candidates_is_noop_without_opening_selection()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();

        await PlayWithEnergy(card);

        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Full_hand_overflow_still_grants_link_before_add_to_hand()
    {
        await ClearCombatPiles();

        var selected = await CreateCardInPile<HiroAttack>(PileType.Draw);
        await FillHandToMax();
        var card = Combat.CreateCard<LingeringAuroraLink>(Player);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([selected]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), selected.Pile);
        Assert.True(((CardModel)selected).HasComponent<LinkComponent>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgrade_removes_exhaust_without_changing_selection_count()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LingeringAuroraLink>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var selected = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var unselected = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var selector = new CapturingCardSelector([selected]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Equal(1, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), selected.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), unselected.Pile);
        Assert.Empty(PileType.Exhaust.GetPile(Player).Cards.OfType<LingeringAuroraLink>());
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
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
