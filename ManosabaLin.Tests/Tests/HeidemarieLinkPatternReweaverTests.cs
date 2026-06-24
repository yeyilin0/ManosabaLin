using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
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

public sealed class HeidemarieLinkPatternReweaverTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-link-pattern-reweaver");
    }

    [Fact]
    public async Task Play_recalls_aurora_sword_from_discard()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LinkPatternReweaver>();
        var aurora = await CreateCardInPile<AuroraSword>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), aurora.Pile);
        Assert.Contains(aurora, PileType.Hand.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Play_recalls_crimson_sword_from_discard()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LinkPatternReweaver>();
        var crimson = await CreateCardInPile<CrimsonSword>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), crimson.Pile);
        Assert.Contains(crimson, PileType.Hand.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Recalled_sword_gains_bounce()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LinkPatternReweaver>();
        var aurora = await CreateCardInPile<AuroraSword>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), aurora.Pile);
        AssertBounce(aurora, 1m);
    }

    [Fact]
    public async Task No_aurora_or_crimson_candidates_is_noop()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LinkPatternReweaver>();
        var nonCandidate = await CreateCardInPile<HiroDefend>(PileType.Discard);
        nonCandidate.TryAddComponent(new SwordGraveComponent());

        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), nonCandidate.Pile);
        Assert.Null(GetBounce(nonCandidate));
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<CrimsonSword>());
    }

    [Fact]
    public async Task Full_hand_still_adds_bounce_to_selected_sword()
    {
        await ClearCombatPiles();

        var card = await CreateCardInPile<LinkPatternReweaver>(PileType.Draw);
        await FillHandToMax();
        var aurora = await CreateCardInPile<AuroraSword>(PileType.Discard);

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), aurora.Pile);
        AssertBounce(aurora, 1m);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgrade_recalls_two_swords_when_available()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LinkPatternReweaver>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var aurora = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var crimson = await CreateCardInPile<CrimsonSword>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), aurora.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), crimson.Pile);
        AssertBounce(aurora, 1m);
        AssertBounce(crimson, 1m);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
        await WaitForIdle();
    }

    private async Task FillHandToMax()
    {
        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();
    }

    private static void AssertBounce(CardModel card, decimal expectedAmount)
    {
        var bounce = GetBounce(card);
        Assert.NotNull(bounce);
        Assert.Equal(expectedAmount, bounce.Amount);
    }

    private static BounceComponent? GetBounce(CardModel card)
    {
        return Assert.IsAssignableFrom<IComponentsCardModel>(card).GetComponent<BounceComponent>();
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
