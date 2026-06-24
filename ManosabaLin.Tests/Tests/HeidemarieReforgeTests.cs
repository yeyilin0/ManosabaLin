using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
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

public sealed class HeidemarieReforgeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-reforge");
    }

    [Fact]
    public async Task Play_can_relink_card_whose_link_was_removed_without_discarding_it()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Reforge>();
        var oldLinked = await AddToHand<HiroDefend>();
        oldLinked.TryAddComponent(new LinkComponent());

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), oldLinked.Pile);
        Assert.True(HasLink(oldLinked));
    }

    [Fact]
    public async Task Play_removes_link_from_unselected_hand_cards_and_requires_full_selection_count()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Reforge>();
        var selected = await AddToHand<HiroAttack>();
        var unselected = await AddToHand<HiroDefend>();
        selected.TryAddComponent(new LinkComponent());
        unselected.TryAddComponent(new LinkComponent());
        var selector = new CapturingCardSelector([selected]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(selector.WasCalled);
        Assert.Equal(1, selector.MinSelect);
        Assert.Equal(1, selector.MaxSelect);
        Assert.Contains(selected, selector.Options);
        Assert.Contains(unselected, selector.Options);
        Assert.Same(PileType.Hand.GetPile(Player), selected.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unselected.Pile);
        Assert.True(HasLink(selected));
        Assert.False(HasLink(unselected));
    }

    [Fact]
    public async Task Play_grants_link_to_selected_unlinked_card_after_cleanup()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Reforge>();
        var selected = await AddToHand<HiroAttack>();

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), selected.Pile);
        Assert.True(HasLink(selected));
    }

    [Fact]
    public async Task No_candidates_is_noop_without_opening_selection()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Reforge>();

        await PlayWithEnergy(card);

        Assert.Empty(PileType.Hand.GetPile(Player).Cards);
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Upgrade_reduces_cost()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Reforge>();

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(1, card.DynamicVars.Cards.IntValue);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
        await WaitForIdle();
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

    private static bool HasLink(CardModel card)
    {
        return ((IComponentsCardModel)card).GetComponent<LinkComponent>() != null;
    }

    private sealed class CapturingCardSelector(IReadOnlyList<CardModel> selectedCards) : ICardSelector
    {
        public bool WasCalled { get; private set; }
        public int MinSelect { get; private set; }
        public int MaxSelect { get; private set; }
        public IReadOnlyList<CardModel> Options { get; private set; } = [];

        public Task<IEnumerable<CardModel>> GetSelectedCards(
            IEnumerable<CardModel> options,
            int minSelect,
            int maxSelect)
        {
            WasCalled = true;
            MinSelect = minSelect;
            MaxSelect = maxSelect;
            Options = options.ToArray();
            return Task.FromResult<IEnumerable<CardModel>>(selectedCards);
        }

        public CardRewardSelection GetSelectedCardReward(
            IReadOnlyList<CardCreationResult> options,
            IReadOnlyList<CardRewardAlternative> alternatives)
        {
            throw new NotSupportedException();
        }
    }
}
