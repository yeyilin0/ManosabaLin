using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard37Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-037");
    }

    [Fact]
    public async Task Playing_searches_rest_cards_from_draw_pile_and_can_discard_drawn_card()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard37>();
        var keptCard = await AddToHand<HiroDefend>();
        var nonRest = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var restCard = await CreateRestCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([restCard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.True(((CardModel)card).HasComponent<RestComponent>());
        Assert.Same(PileType.Draw.GetPile(Player), nonRest.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), restCard.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), keptCard.Pile);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, restCard) && entry.CardPlay.IsAutoPlay);
    }

    [Fact]
    public async Task No_rest_candidates_still_discards_selected_hand_card()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard37>();
        var discarded = await AddToHand<HiroAttack>();
        var keptCard = await AddToHand<HiroDefend>();
        var nonRest = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Draw.GetPile(Player), nonRest.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), keptCard.Pile);
    }

    [Fact]
    public async Task Upgraded_play_draws_and_discards_two_rest_cards()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard37>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var keptCard = await AddToHand<HiroDefend>();
        var firstRest = await CreateRestCardInPile<HiroAttack>(PileType.Draw);
        var secondRest = await CreateRestCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([firstRest, secondRest]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), firstRest.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), secondRest.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), keptCard.Pile);
    }

    [Fact]
    public async Task Rest_discard_autoplay_runs_same_draw_and_discard_effect()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard37>();
        var discarded = await AddToHand<HiroAttack>();
        var restCard = await CreateRestCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), restCard.Pile);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Empty_draw_pile_and_empty_hand_are_safe()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard37>();

        await PlayWithEnergy(card);

        Assert.Empty(PileType.Hand.GetPile(Player).Cards);
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
        await WaitForIdle();
    }

    private async Task<TCard> CreateRestCardInPile<TCard>(PileType pileType)
        where TCard : CardModel
    {
        var card = await CreateCardInPile<TCard>(pileType);
        card.TryAddComponent(new RestComponent());
        return card;
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await Play(actionCard, EnemyAt(0));
        await WaitForIdle();
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
}
