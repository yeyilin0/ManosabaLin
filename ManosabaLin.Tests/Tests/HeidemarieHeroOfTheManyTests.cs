using ManosabaLin.Characters.Common.Components;
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
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieHeroOfTheManyTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-hero-of-the-many");
    }

    [Fact]
    public async Task Selected_name_links_and_moves_matching_cards_from_standard_piles_to_draw()
    {
        await ClearCombatPiles();

        var card = await AddToHand<HeroOfTheMany>();
        var handMatch = await AddToHand<HiroDefend>();
        var drawMatch = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var discardMatch = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var exhaustMatch = await CreateCardInPile<HiroDefend>(PileType.Exhaust);
        var nonMatch = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var selector = new CapturingCardSelector([handMatch]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Contains(handMatch, selector.Options);
        Assert.Contains(drawMatch, selector.Options);
        Assert.Contains(discardMatch, selector.Options);
        Assert.Contains(exhaustMatch, selector.Options);

        foreach (var match in new[] { handMatch, drawMatch, discardMatch, exhaustMatch })
        {
            Assert.Same(PileType.Draw.GetPile(Player), match.Pile);
            Assert.True(HasLink(match));
        }

        Assert.Same(PileType.Discard.GetPile(Player), nonMatch.Pile);
        Assert.False(HasLink(nonMatch));
        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Upgraded_card_can_select_two_names()
    {
        await ClearCombatPiles();

        var card = await AddToHand<HeroOfTheMany>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var defend = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var attack = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var selector = new CapturingCardSelector([defend, attack]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(2, selector.MaxSelect);
        Assert.Same(PileType.Draw.GetPile(Player), defend.Pile);
        Assert.Same(PileType.Draw.GetPile(Player), attack.Pile);
        Assert.True(HasLink(defend));
        Assert.True(HasLink(attack));
    }

    [Fact]
    public async Task Upgraded_card_caps_to_available_candidates_without_forcing_selection()
    {
        await ClearCombatPiles();

        var card = await AddToHand<HeroOfTheMany>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var selector = new CapturingCardSelector([]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(0, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Contains(card, selector.Options);
        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Multiple_selected_names_are_unioned_once_and_moved_in_stable_order()
    {
        await ClearCombatPiles();

        var card = await AddToHand<HeroOfTheMany>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var handDefend = await AddToHand<HiroDefend>();
        var drawDefend = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var discardAttack = await CreateCardInPile<HiroAttack>(PileType.Discard);
        var exhaustAttack = await CreateCardInPile<HiroAttack>(PileType.Exhaust);
        var selector = new CapturingCardSelector([discardAttack, handDefend, drawDefend]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        AssertDrawOrder([discardAttack, exhaustAttack, handDefend, drawDefend]);
    }

    [Fact]
    public async Task Selected_current_card_name_includes_resolving_card_and_exhaust_matches()
    {
        await ClearCombatPiles();

        var card = await AddToHand<HeroOfTheMany>();
        var exhaustCopy = await CreateCardInPile<HeroOfTheMany>(PileType.Exhaust);
        var selector = new CapturingCardSelector([card]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Contains(card, selector.Options);
        Assert.Same(PileType.Draw.GetPile(Player), card.Pile);
        Assert.Same(PileType.Draw.GetPile(Player), exhaustCopy.Pile);
        Assert.True(HasLink(card));
        Assert.True(HasLink(exhaustCopy));
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
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust, PileType.Play })
        {
            var cards = pileType.GetPile(Player).Cards.ToArray();
            if (cards.Length > 0)
                await CardPileCmd.RemoveFromCombat(cards, skipVisuals: true);
        }

        await WaitForIdle();
    }

    private void AssertDrawOrder(IReadOnlyList<CardModel> expected)
    {
        var drawCards = PileType.Draw.GetPile(Player).Cards;
        var affected = drawCards
            .Where(card => expected.Any(expectedCard => ReferenceEquals(expectedCard, card)))
            .ToArray();

        Assert.Equal(expected, affected);
        foreach (var expectedCard in expected)
        {
            Assert.Equal(
                1,
                drawCards.Count(card => ReferenceEquals(expectedCard, card)));
            Assert.True(HasLink(expectedCard));
        }
    }

    private static bool HasLink(CardModel card)
    {
        return ((IComponentsCardModel)card).GetComponent<LinkComponent>() != null;
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
