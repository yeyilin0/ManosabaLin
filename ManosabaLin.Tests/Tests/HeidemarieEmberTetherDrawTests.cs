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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieEmberTetherDrawTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-ember-tether-draw");
    }

    [Fact]
    public async Task Current_rest_cards_gain_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberTetherDraw>();
        var restCard = await AddRestCardToHand();

        await PlayWithEnergy(card);

        Assert.True(HasLink(restCard));
    }

    [Fact]
    public async Task Non_rest_cards_do_not_gain_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberTetherDraw>();
        var nonRest = await AddToHand<HiroDefend>();

        await PlayWithEnergy(card);

        Assert.False(HasLink(nonRest));
    }

    [Fact]
    public async Task Future_rest_card_entering_hand_gains_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberTetherDraw>();
        var futureRest = await CreateCardInPile<HiroDefend>(PileType.Draw);
        futureRest.TryAddComponent(new RestComponent());

        await PlayWithEnergy(card);
        Assert.False(HasLink(futureRest));

        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), futureRest.Pile);
        Assert.True(HasLink(futureRest));
    }

    [Fact]
    public async Task Turn_end_removes_listener()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberTetherDraw>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<EmberTetherDrawPower>();
        Assert.NotNull(power);

        await power.AfterSideTurnEnd(new BlockingPlayerChoiceContext(), CombatSide.Player, [Player.Creature]);
        await WaitForIdle();

        Assert.Null(Player.Creature.GetPower<EmberTetherDrawPower>());

        var futureRest = await CreateCardInPile<HiroDefend>(PileType.Discard);
        futureRest.TryAddComponent(new RestComponent());

        await CardPileCmd.Add(futureRest, PileType.Hand);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), futureRest.Pile);
        Assert.False(HasLink(futureRest));
    }

    [Fact]
    public async Task Upgrade_only_lowers_cost()
    {
        var card = await AddToHand<EmberTetherDraw>();

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        await PlayWithEnergy(card);
    }

    [Fact]
    public async Task Card_exhausts_itself()
    {
        await ClearCombatPiles();

        var card = await AddToHand<EmberTetherDraw>();

        await PlayWithEnergy(card);

        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task<CardModel> AddRestCardToHand()
    {
        var card = await AddToHand<HiroDefend>();
        card.TryAddComponent(new RestComponent());
        return card;
    }

    private static bool HasLink(CardModel card)
    {
        return card.HasComponent<LinkComponent>();
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
