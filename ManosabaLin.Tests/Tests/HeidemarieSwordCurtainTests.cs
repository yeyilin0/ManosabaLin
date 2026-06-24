using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieSwordCurtainTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-sword-curtain");
    }

    [Fact]
    public async Task Play_installs_sword_curtain_power()
    {
        await ClearCombatPiles();

        var card = await AddToHand<SwordCurtain>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<SwordCurtainPower>();
        Assert.NotNull(power);
        Assert.Equal(card.DynamicVars[SwordCurtain.BlockPerAuroraKey].BaseValue, power.Amount);
    }

    [Fact]
    public async Task Aurora_generation_grants_block()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainPower>(Player.Creature, 1, Player.Creature);

        var blockBefore = Player.Creature.Block;
        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Aurora));
        Assert.Equal(blockBefore + 1, Player.Creature.Block);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Crimson_generation_does_not_grant_block()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainPower>(Player.Creature, 3, Player.Creature);

        var blockBefore = Player.Creature.Block;
        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Crimson));
        Assert.Equal(blockBefore, Player.Creature.Block);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Multiple_aurora_swords_trigger_block_once_each()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainPower>(Player.Creature, 1, Player.Creature);

        var blockBefore = Player.Creature.Block;
        var blockEntriesBefore = BlockGainEntriesForPlayer().Count();

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            3);
        await WaitForIdle();

        Assert.Equal(3, result.SuccessCountFor(SwordTokenKind.Aurora));
        Assert.Equal(blockBefore + 3, Player.Creature.Block);
        Assert.Equal(blockEntriesBefore + 3, BlockGainEntriesForPlayer().Count());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Multiple_stacks_increase_block_per_aurora()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainPower>(Player.Creature, 2, Player.Creature);

        var blockBefore = Player.Creature.Block;
        await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(blockBefore + 2, Player.Creature.Block);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Full_hand_overflow_still_counts_as_successful_aurora_generation()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainPower>(Player.Creature, 1, Player.Creature);

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<LinkedEdge>();

        var blockBefore = Player.Creature.Block;
        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Aurora));
        Assert.Equal(0, result.EnteredHandCountFor(SwordTokenKind.Aurora));
        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Single(PileType.Discard.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Equal(blockBefore + 1, Player.Creature.Block);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Original_aurora_replaced_with_crimson_does_not_trigger()
    {
        await ClearCombatPiles();
        await ApplyPower<SwordCurtainAuroraToCrimsonReplacementPower>(Player.Creature, 1, Player.Creature);
        await ApplyPower<SwordCurtainPower>(Player.Creature, 1, Player.Creature);

        var blockBefore = Player.Creature.Block;
        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Aurora, result.Request.OriginalKind);
        Assert.Equal(0, result.SuccessCountFor(SwordTokenKind.Aurora));
        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Crimson));
        Assert.Equal(blockBefore, Player.Creature.Block);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_card_installs_two_block_per_aurora()
    {
        await ClearCombatPiles();

        var card = await AddToHand<SwordCurtain>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<SwordCurtainPower>();
        Assert.NotNull(power);
        Assert.Equal(2, power.Amount);

        var blockBefore = Player.Creature.Block;
        await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(blockBefore + 2, Player.Creature.Block);
    }

    private IEnumerable<BlockGainedEntry> BlockGainEntriesForPlayer()
    {
        return CombatManager.Instance.History.Entries
            .OfType<BlockGainedEntry>()
            .Where(entry => ReferenceEquals(entry.Receiver, Player.Creature));
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
        var actionCard = hand.Cards.OfType<LinkedEdge>().FirstOrDefault()
            ?? await AddToHand<LinkedEdge>();

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

[RegisterPower]
public sealed class SwordCurtainAuroraToCrimsonReplacementPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        if (request.OriginalKind == SwordTokenKind.Aurora)
            request.ReplaceWith(SwordTokenKind.Crimson, request.OriginalCount);

        return Task.CompletedTask;
    }
}
