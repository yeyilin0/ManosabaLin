using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieTwinEdgeSlumberTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-twin-edge-slumber");
    }

    [Fact]
    public async Task Base_play_generates_one_aurora_sword_with_one_bounce()
    {
        await ClearCombatPiles();

        var card = await AddToHand<TwinEdgeSlumber>();

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        var generated = Assert.Single(AuroraSwordsIn(PileType.Hand));
        AssertBounce(generated, 1m);
        Assert.False(HasComponent<RestComponent>(card));
    }

    [Fact]
    public async Task Upgraded_play_generates_base_aurora_sword_with_base_bounce_and_has_rest()
    {
        await ClearCombatPiles();

        var card = await AddToHand<TwinEdgeSlumber>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        Assert.True(HasComponent<RestComponent>(card));
        var generated = AuroraSwordsIn(PileType.Hand);
        var sword = Assert.Single(generated);
        AssertBounce(sword, 1m);
    }

    [Fact]
    public async Task AutoPlay_with_full_hand_overflows_base_sword_to_discard_with_bounce()
    {
        await ClearCombatPiles();

        var card = await CreateCardInPile<TwinEdgeSlumber>(PileType.Draw);
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        await FillHandToMax();

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        var discardSword = Assert.Single(AuroraSwordsIn(PileType.Discard));
        Assert.Empty(AuroraSwordsIn(PileType.Hand));
        AssertBounce(discardSword, 1m);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Rest_discard_autoplay_generates_same_bounced_aurora_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<TwinEdgeSlumber>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
        var generated = AuroraSwordsIn(PileType.Hand);
        var sword = Assert.Single(generated);
        AssertBounce(sword, 1m);

        await ExecuteRunnerAction();
    }

    private List<AuroraSword> AuroraSwordsIn(PileType pileType)
    {
        return pileType.GetPile(Player).Cards.OfType<AuroraSword>().ToList();
    }

    private static void AssertBounce(CardModel card, decimal expectedAmount)
    {
        var bounce = ((IComponentsCardModel)card).GetComponent<BounceComponent>();
        Assert.NotNull(bounce);
        Assert.Equal(expectedAmount, bounce.Amount);
    }

    private static bool HasComponent<T>(CardModel card)
        where T : class, ICardComponent
    {
        return ((IComponentsCardModel)card).GetComponent<T>() != null;
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
