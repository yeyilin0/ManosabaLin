using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieLinkDiscardContextTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Hiro>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-link-discard-context");
    }

    [Fact]
    public async Task Link_cleanup_discard_context_is_true_for_discarded_card()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var played = await AddLinkedProbe(100);
        var discarded = await AddLinkedProbe(1);

        await Play(played);

        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        var record = Assert.Single(LinkDiscardContextProbeCard.Discards);
        Assert.Equal(1, record.Marker);
        Assert.True(record.IsActiveForSelf);
        Assert.True(record.ContainsSelf);
    }

    [Fact]
    public async Task Ordinary_discard_context_is_false()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var discarded = await AddProbe(1);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), discarded);
        await WaitForIdle();

        var record = Assert.Single(LinkDiscardContextProbeCard.Discards);
        Assert.Equal(1, record.Marker);
        Assert.False(record.IsActiveForSelf);
        Assert.False(record.ContainsSelf);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task AutoPlay_does_not_trigger_link_cleanup_context()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var played = await AddLinkedProbe(1);
        var otherLinked = await AddLinkedProbe(2);

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            played,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        var play = Assert.Single(LinkDiscardContextProbeCard.Plays);
        Assert.Equal(1, play.Marker);
        Assert.True(play.IsAutoPlay);
        Assert.False(play.IsActiveForSelf);
        Assert.Empty(LinkDiscardContextProbeCard.Discards);
        Assert.Same(PileType.Hand.GetPile(Player), otherLinked.Pile);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Link_cleanup_context_contains_every_card_in_batch()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var played = await AddLinkedProbe(100);
        var first = await AddLinkedProbe(1);
        var second = await AddLinkedProbe(2);

        await Play(played);

        Assert.Same(PileType.Discard.GetPile(Player), first.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), second.Pile);
        Assert.Equal([1, 2], LinkDiscardContextProbeCard.Discards.Select(record => record.Marker));
        Assert.All(LinkDiscardContextProbeCard.Discards, record => Assert.True(record.IsActiveForSelf));
        Assert.All(LinkDiscardContextProbeCard.Discards, record => Assert.True(record.ContainsSelf));
    }

    [Fact]
    public async Task Nested_scopes_keep_parent_cards_active_and_restore_after_dispose()
    {
        var outer = Combat.CreateCard<LinkDiscardContextProbeCard>(Player);
        var inner = Combat.CreateCard<LinkDiscardContextProbeCard>(Player);

        using (LinkDiscardContext.Begin([outer]))
        {
            Assert.True(LinkDiscardContext.IsActiveFor(outer));
            Assert.False(LinkDiscardContext.IsActiveFor(inner));

            using (LinkDiscardContext.Begin([inner]))
            {
                await Task.Yield();
                Assert.True(LinkDiscardContext.IsActiveFor(outer));
                Assert.True(LinkDiscardContext.IsActiveFor(inner));
            }

            Assert.True(LinkDiscardContext.IsActiveFor(outer));
            Assert.False(LinkDiscardContext.IsActiveFor(inner));
        }

        Assert.False(LinkDiscardContext.IsActiveFor(outer));
        Assert.False(LinkDiscardContext.IsActiveFor(inner));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Async_scopes_do_not_leak_between_sibling_flows()
    {
        var first = Combat.CreateCard<LinkDiscardContextProbeCard>(Player);
        var second = Combat.CreateCard<LinkDiscardContextProbeCard>(Player);

        var firstResult = Task.Run(() => ObserveAsyncScope(first, second));
        var secondResult = Task.Run(() => ObserveAsyncScope(second, first));

        var results = await Task.WhenAll(firstResult, secondResult);

        Assert.All(results, result => Assert.True(result.SelfActive));
        Assert.All(results, result => Assert.False(result.OtherActive));
        Assert.False(LinkDiscardContext.IsActiveFor(first));
        Assert.False(LinkDiscardContext.IsActiveFor(second));

        await ExecuteRunnerAction();
    }

    private async Task<LinkDiscardContextProbeCard> AddLinkedProbe(int marker)
    {
        var card = await AddProbe(marker);
        card.TryAddComponent(new LinkComponent());
        return card;
    }

    private async Task<LinkDiscardContextProbeCard> AddProbe(int marker)
    {
        var card = await AddToHand<LinkDiscardContextProbeCard>();
        card.Marker = marker;
        return card;
    }

    private static async Task<AsyncScopeObservation> ObserveAsyncScope(CardModel self, CardModel other)
    {
        using var scope = LinkDiscardContext.Begin([self]);
        await Task.Yield();
        return new AsyncScopeObservation(
            LinkDiscardContext.IsActiveFor(self),
            LinkDiscardContext.IsActiveFor(other));
    }

    private static void ResetProbeState()
    {
        LinkDiscardContextProbeCard.Discards.Clear();
        LinkDiscardContextProbeCard.Plays.Clear();
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddProbe(999);
        await Play(actionCard);
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

public sealed record LinkDiscardProbeDiscard(int Marker, bool IsActiveForSelf, bool ContainsSelf);

public sealed record LinkDiscardProbePlay(int Marker, bool IsAutoPlay, bool IsActiveForSelf);

public sealed record AsyncScopeObservation(bool SelfActive, bool OtherActive);

[RegisterCard(typeof(LinCardPool))]
public sealed class LinkDiscardContextProbeCard()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public static readonly List<LinkDiscardProbeDiscard> Discards = [];
    public static readonly List<LinkDiscardProbePlay> Plays = [];

    public int Marker { get; set; }

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        Plays.Add(new LinkDiscardProbePlay(Marker, cardPlay.IsAutoPlay, LinkDiscardContext.IsActiveFor(this)));
        return Task.CompletedTask;
    }

    protected override Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this))
        {
            Discards.Add(new LinkDiscardProbeDiscard(
                Marker,
                LinkDiscardContext.IsActiveFor(this),
                LinkDiscardContext.Contains(this)));
        }

        return Task.CompletedTask;
    }
}
