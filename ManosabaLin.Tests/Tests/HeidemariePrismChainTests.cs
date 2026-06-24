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

public sealed class HeidemariePrismChainTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-prism-chain");
    }

    [Fact]
    public async Task Drawn_card_can_be_selected_and_gains_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<PrismChain>();
        var drawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new CapturingCardSelector([drawn]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Contains(drawn, selector.Options);
        Assert.Same(PileType.Hand.GetPile(Player), drawn.Pile);
        Assert.True(((CardModel)drawn).HasComponent<LinkComponent>());
    }

    [Fact]
    public async Task Existing_link_cards_are_excluded_and_cleanup_uses_play_start_snapshot()
    {
        await ClearCombatPiles();

        var card = await AddToHand<PrismChain>();
        var oldLinked = await AddToHand<HiroDefend>();
        var drawn = await CreateCardInPile<HiroAttack>(PileType.Draw);
        oldLinked.TryAddComponent(new LinkComponent());
        var selector = new CapturingCardSelector([drawn]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.DoesNotContain(oldLinked, selector.Options);
        Assert.Same(PileType.Discard.GetPile(Player), oldLinked.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), drawn.Pile);
        Assert.True(((CardModel)drawn).HasComponent<LinkComponent>());
    }

    [Fact]
    public async Task No_selectable_hand_cards_does_not_open_selection()
    {
        await ClearCombatPiles();

        var card = await AddToHand<PrismChain>();
        var selector = new CapturingCardSelector([]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.False(selector.WasCalled);
    }

    [Fact]
    public async Task Upgraded_card_can_select_two_without_drawing_extra_cards()
    {
        await ClearCombatPiles();

        var card = await AddToHand<PrismChain>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var existing = await AddToHand<HiroDefend>();
        var drawn = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var notDrawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new CapturingCardSelector([existing, drawn]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(2, selector.MaxSelect);
        Assert.True(((CardModel)existing).HasComponent<LinkComponent>());
        Assert.True(((CardModel)drawn).HasComponent<LinkComponent>());
        Assert.Same(PileType.Draw.GetPile(Player), notDrawn.Pile);
    }

    [Fact]
    public async Task Upgraded_card_does_not_force_selecting_available_candidates()
    {
        await ClearCombatPiles();

        var card = await AddToHand<PrismChain>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var candidate = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new CapturingCardSelector([]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.False(((CardModel)candidate).HasComponent<LinkComponent>());
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
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
