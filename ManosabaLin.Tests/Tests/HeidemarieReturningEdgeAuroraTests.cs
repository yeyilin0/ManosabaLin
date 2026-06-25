using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieReturningEdgeAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-returning-edge-aurora");
    }

    [Fact]
    public async Task Play_installs_returning_edge_aurora_power()
    {
        await ClearCombatPiles();

        var card = await AddToHand<ReturningEdgeAurora>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<ReturningEdgeAuroraPower>();
        Assert.NotNull(power);
        Assert.Equal(card.DynamicVars[ReturningEdgeAurora.AuroraSwordCountKey].BaseValue, power.Amount);
    }

    [Fact]
    public async Task Bounce_from_discard_success_generates_aurora_sword()
    {
        await ClearCombatPiles();
        await ApplyPower<ReturningEdgeAuroraPower>(Player.Creature, 1, Player.Creature);

        var bouncingCard = await CreateBouncingCard(PileType.Discard);

        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Hand.GetPile(Player), bouncingCard.Pile);
        Assert.Null(GetBounce(bouncingCard));
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Ordinary_move_of_bounce_card_to_hand_does_not_trigger()
    {
        await ClearCombatPiles();
        await ApplyPower<ReturningEdgeAuroraPower>(Player.Creature, 1, Player.Creature);

        var bouncingCard = await CreateBouncingCard(PileType.Discard);

        await CardPileCmd.Add(bouncingCard, PileType.Hand);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), bouncingCard.Pile);
        Assert.Equal(1, GetBounce(bouncingCard)?.Amount);
        Assert.Equal(0, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(0, CountCards<AuroraSword>(PileType.Discard));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Full_hand_bounce_failure_does_not_trigger_or_consume_bounce()
    {
        await ClearCombatPiles();
        await ApplyPower<ReturningEdgeAuroraPower>(Player.Creature, 1, Player.Creature);

        var bouncingCard = await CreateBouncingCard(PileType.Discard);
        await FillHandToMax();

        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Discard.GetPile(Player), bouncingCard.Pile);
        Assert.Equal(1, GetBounce(bouncingCard)?.Amount);
        Assert.Equal(0, CountCombatCards<AuroraSword>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Multiple_bounce_cards_trigger_one_aurora_sword_each()
    {
        await ClearCombatPiles();
        await ApplyPower<ReturningEdgeAuroraPower>(Player.Creature, 1, Player.Creature);

        var discardCard = await CreateBouncingCard(PileType.Discard);
        var drawCard = await CreateBouncingCard(PileType.Draw);

        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Hand.GetPile(Player), discardCard.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), drawCard.Pile);
        Assert.Equal(2, CountCards<AuroraSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Repeated_play_stacks_power_and_generates_per_stack()
    {
        await ClearCombatPiles();

        await PlayWithEnergy(await AddToHand<ReturningEdgeAurora>());
        await PlayWithEnergy(await AddToHand<ReturningEdgeAurora>());

        var power = Player.Creature.GetPower<ReturningEdgeAuroraPower>();
        Assert.NotNull(power);
        Assert.Equal(2, power.Amount);

        await CreateBouncingCard(PileType.Discard);
        await TriggerPlayerTurnStartHooks();

        Assert.Equal(2, CountCards<AuroraSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_card_only_lowers_cost()
    {
        await ClearCombatPiles();

        var card = await AddToHand<ReturningEdgeAurora>();
        var baseAmount = card.DynamicVars[ReturningEdgeAurora.AuroraSwordCountKey].BaseValue;

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(baseAmount, card.DynamicVars[ReturningEdgeAurora.AuroraSwordCountKey].BaseValue);

        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<ReturningEdgeAuroraPower>();
        Assert.NotNull(power);
        Assert.Equal(baseAmount, power.Amount);
    }

    private async Task<CardModel> CreateBouncingCard(PileType pileType)
    {
        var card = await CreateCardInPile<HiroDefend>(pileType);
        card.TryAddComponent(new BounceComponent(1));
        return card;
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

    private async Task TriggerPlayerTurnStartHooks()
    {
        await Hook.BeforeSideTurnStart(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();
    }

    private async Task FillHandToMax()
    {
        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
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

    private int CountCombatCards<TCard>()
        where TCard : CardModel
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .Sum(CountCards<TCard>);
    }

    private int CountCards<TCard>(PileType pileType)
        where TCard : CardModel
    {
        return pileType.GetPile(Player).Cards.OfType<TCard>().Count();
    }

    private static BounceComponent? GetBounce(CardModel card)
    {
        return Assert.IsAssignableFrom<IComponentsCardModel>(card).GetComponent<BounceComponent>();
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
