using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieGlimmerHarvestTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-glimmer-harvest");
    }

    [Fact]
    public async Task Drawn_by_this_effect_gains_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        var drawn = await CreateCardInPile<HiroDefend>(PileType.Draw);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), drawn.Pile);
        Assert.True(HasComponent<LinkComponent>(drawn));
    }

    [Fact]
    public async Task Later_drawn_cards_this_turn_gain_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        var firstDrawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var laterDrawn = await CreateCardInPile<HiroAttack>(PileType.Draw);

        await PlayWithEnergy(card);
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player);
        await WaitForIdle();

        Assert.True(HasComponent<LinkComponent>(firstDrawn));
        Assert.Same(PileType.Hand.GetPile(Player), laterDrawn.Pile);
        Assert.True(HasComponent<LinkComponent>(laterDrawn));
    }

    [Fact]
    public async Task Non_draw_hand_entry_does_not_gain_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        var drawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var moved = await CreateCardInPile<HiroAttack>(PileType.Discard);

        await PlayWithEnergy(card);
        await CardPileCmd.Add(moved, PileType.Hand);
        await WaitForIdle();

        Assert.True(HasComponent<LinkComponent>(drawn));
        Assert.Same(PileType.Hand.GetPile(Player), moved.Pile);
        Assert.False(HasComponent<LinkComponent>(moved));
    }

    [Fact]
    public async Task Newly_drawn_linked_cards_are_not_cleaned_up_by_current_link_snapshot()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        var oldLinked = await AddToHand<HiroDefend>();
        var drawn = await CreateCardInPile<HiroAttack>(PileType.Draw);
        oldLinked.TryAddComponent(new LinkComponent());

        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), oldLinked.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), drawn.Pile);
        Assert.True(HasComponent<LinkComponent>(drawn));
    }

    [Fact]
    public async Task Upgraded_rest_discard_autoplay_triggers_same_effect()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var drawn = await CreateCardInPile<HiroDefend>(PileType.Draw);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.True(HasComponent<RestComponent>(card));
        Assert.Same(PileType.Hand.GetPile(Player), drawn.Pile);
        Assert.True(HasComponent<LinkComponent>(drawn));
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Draw_listener_is_removed_at_turn_end()
    {
        await ClearCombatPiles();

        var card = await AddToHand<GlimmerHarvest>();
        var firstDrawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var afterTurnEndDrawn = await CreateCardInPile<HiroAttack>(PileType.Draw);

        await PlayWithEnergy(card);
        await TriggerPlayerTurnEndHooks();
        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player);
        await WaitForIdle();

        Assert.True(HasComponent<LinkComponent>(firstDrawn));
        Assert.False(Player.Creature.HasPower<GlimmerHarvestPower>());
        Assert.Same(PileType.Hand.GetPile(Player), afterTurnEndDrawn.Pile);
        Assert.False(HasComponent<LinkComponent>(afterTurnEndDrawn));
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task TriggerPlayerTurnEndHooks()
    {
        await Hook.AfterTurnEnd(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();
    }

    private static bool HasComponent<T>(CardModel card)
        where T : class, ICardComponent
    {
        return ((IComponentsCardModel)card).GetComponent<T>() != null;
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
