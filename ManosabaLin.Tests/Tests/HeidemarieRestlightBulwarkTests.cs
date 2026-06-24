using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieRestlightBulwarkTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-restlight-bulwark");
    }

    [Fact]
    public async Task Playing_gains_base_block()
    {
        await ClearCombatPiles();
        await PlayerCmd.SetEnergy(10, Player);

        var card = await AddToHand<RestlightBulwark>();
        var blockBefore = Player.Creature.Block;

        await Play(card);

        Assert.Equal(blockBefore + ExpectedBlock(card, 0), Player.Creature.Block);
    }

    [Fact]
    public async Task Playing_gains_extra_block_for_linked_cards_in_hand()
    {
        await ClearCombatPiles();
        await PlayerCmd.SetEnergy(10, Player);

        var card = await AddToHand<RestlightBulwark>();
        var linked = await AddToHand<HiroDefend>();
        var unlinked = await AddToHand<HiroDefend>();
        linked.TryAddComponent(new LinkComponent());
        var blockBefore = Player.Creature.Block;

        await Play(card);

        Assert.Equal(blockBefore + ExpectedBlock(card, 1), Player.Creature.Block);
        Assert.Same(PileType.Hand.GetPile(Player), unlinked.Pile);
    }

    [Fact]
    public async Task Rest_discard_autoplay_gains_block()
    {
        await ClearCombatPiles();

        var card = await AddToHand<RestlightBulwark>();
        var blockBefore = Player.Creature.Block;

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Equal(blockBefore + ExpectedBlock(card, 0), Player.Creature.Block);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);

        await PlayCleanupAction();
    }

    [Fact]
    public async Task Link_cleanup_rest_uses_pre_cleanup_link_snapshot()
    {
        await ClearCombatPiles();
        await PlayerCmd.SetEnergy(10, Player);

        var played = await AddToHand<LinkedEdge>();
        var discardedFirst = await AddToHand<HiroDefend>();
        var bulwark = await AddToHand<RestlightBulwark>();
        discardedFirst.TryAddComponent(new LinkComponent());
        bulwark.TryAddComponent(new LinkComponent());
        var blockBefore = Player.Creature.Block;

        await Play(played, EnemyAt(0));

        Assert.Same(PileType.Discard.GetPile(Player), discardedFirst.Pile);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, bulwark) && entry.CardPlay.IsAutoPlay);
        Assert.Equal(blockBefore + ExpectedBlock(bulwark, 2), Player.Creature.Block);
    }

    [Fact]
    public async Task Upgrade_only_lowers_cost()
    {
        var card = await AddToHand<RestlightBulwark>();
        var block = card.DynamicVars.Block.BaseValue;
        var linkedCardsPerBlock = card.DynamicVars[RestlightBulwark.LinkedCardsPerBlockKey].BaseValue;
        var blockPerLinkedBatch = card.DynamicVars[RestlightBulwark.BlockPerLinkedBatchKey].BaseValue;

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(block, card.DynamicVars.Block.BaseValue);
        Assert.Equal(linkedCardsPerBlock, card.DynamicVars[RestlightBulwark.LinkedCardsPerBlockKey].BaseValue);
        Assert.Equal(blockPerLinkedBatch, card.DynamicVars[RestlightBulwark.BlockPerLinkedBatchKey].BaseValue);
    }

    private static decimal ExpectedBlock(RestlightBulwark card, int linkedCards)
    {
        var linkedCardsPerBlock = card.DynamicVars[RestlightBulwark.LinkedCardsPerBlockKey].IntValue;
        var bonusBatches = linkedCards / linkedCardsPerBlock;
        return card.DynamicVars.Block.BaseValue
            + bonusBatches * card.DynamicVars[RestlightBulwark.BlockPerLinkedBatchKey].BaseValue;
    }

    private async Task PlayCleanupAction()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<BattlemarkBond>());
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
