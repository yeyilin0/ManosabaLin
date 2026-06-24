using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieChainSigilIgnitionTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-chain-sigil-ignition");
    }

    [Fact]
    public async Task No_linked_cards_only_exhausts_chain_sigil()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var card = await AddToHand<ChainSigilIgnition>();
        var unlinked = await AddToHand<ChainSigilIgnitionProbeCard>();

        await PlayWithOneEnergy(card);

        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unlinked.Pile);
        Assert.Empty(ChainSigilIgnitionProbeCard.Plays);
    }

    [Fact]
    public async Task Auto_plays_linked_cards_in_stable_hand_order_without_spending_energy()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var card = await AddToHand<ChainSigilIgnition>();
        var first = await AddLinkedProbe(1);
        var unlinked = await AddProbe(99);
        var second = await AddLinkedProbe(2);

        await PlayWithOneEnergy(card);

        Assert.Equal([1, 2], ChainSigilIgnitionProbeCard.Plays.Select(play => play.Marker));
        Assert.All(ChainSigilIgnitionProbeCard.Plays, play => Assert.True(play.IsAutoPlay));
        Assert.NotNull(Player.PlayerCombatState);
        Assert.Equal(0, Player.PlayerCombatState!.Energy);
        Assert.Same(PileType.Discard.GetPile(Player), first.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), second.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unlinked.Pile);
    }

    [Fact]
    public async Task Skips_snapshotted_cards_that_are_no_longer_linked()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var card = await AddToHand<ChainSigilIgnition>();
        await AddLinkedProbe(1);
        var skipped = await AddLinkedProbe(2);
        ChainSigilIgnitionProbeCard.RemoveLinkFromOnPlay = skipped;

        await PlayWithOneEnergy(card);

        Assert.Equal([1], ChainSigilIgnitionProbeCard.Plays.Select(play => play.Marker));
        Assert.Same(PileType.Hand.GetPile(Player), skipped.Pile);
        Assert.False(((CardModel)skipped).HasComponent<LinkComponent>());
    }

    [Fact]
    public async Task Auto_played_link_cards_do_not_trigger_rest_or_extra_discard()
    {
        await ClearCombatPiles();
        ResetProbeState();

        var card = await AddToHand<ChainSigilIgnition>();
        var linkedRest = await AddLinkedProbe(1);
        linkedRest.TryAddComponent(new RestComponent());

        await PlayWithOneEnergy(card);

        Assert.Equal([1], ChainSigilIgnitionProbeCard.Plays.Select(play => play.Marker));
        Assert.Same(PileType.Discard.GetPile(Player), linkedRest.Pile);
    }

    [Fact]
    public async Task Upgrade_only_lowers_cost()
    {
        var card = await AddToHand<ChainSigilIgnition>();

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        await PlayWithOneEnergy(card);
    }

    private async Task PlayWithOneEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(1, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task<ChainSigilIgnitionProbeCard> AddLinkedProbe(int marker)
    {
        var card = await AddProbe(marker);
        card.TryAddComponent(new LinkComponent());
        return card;
    }

    private async Task<ChainSigilIgnitionProbeCard> AddProbe(int marker)
    {
        var card = await AddToHand<ChainSigilIgnitionProbeCard>();
        card.Marker = marker;
        return card;
    }

    private static void ResetProbeState()
    {
        ChainSigilIgnitionProbeCard.Plays.Clear();
        ChainSigilIgnitionProbeCard.RemoveLinkFromOnPlay = null;
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

public sealed record ChainSigilIgnitionProbePlay(int Marker, bool IsAutoPlay);

[RegisterCard(typeof(LinCardPool))]
public sealed class ChainSigilIgnitionProbeCard()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Token, TargetType.Self, false)
{
    public static readonly List<ChainSigilIgnitionProbePlay> Plays = [];
    public static CardModel? RemoveLinkFromOnPlay { get; set; }

    public int Marker { get; set; }

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        Plays.Add(new ChainSigilIgnitionProbePlay(Marker, cardPlay.IsAutoPlay));
        RemoveLinkFromOnPlay?.TryRemoveComponent<LinkComponent>();
        RemoveLinkFromOnPlay = null;
        return Task.CompletedTask;
    }
}
