using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieFormlessEmberlightTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-formless-emberlight");
    }

    [Fact]
    public async Task Play_installs_only_one_power_layer()
    {
        await ClearCombatPiles();

        var first = await AddToHand<FormlessEmberlight>();
        var second = await AddToHand<FormlessEmberlight>();

        await PlayWithEnergy(first);
        await PlayWithEnergy(second);

        var power = Player.Creature.GetPower<FormlessEmberlightPower>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Equal(FormlessEmberlightPower.BaseSwordCount, power.SwordCount);
    }

    [Fact]
    public async Task Upgraded_card_is_innate_and_uses_two_replacement_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<FormlessEmberlight>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Contains(CardKeyword.Innate, card.Keywords);

        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<FormlessEmberlightPower>();
        Assert.NotNull(power);
        Assert.Equal(2, power.SwordCount);

        var rng = Player.RunState.Rng.CombatCardGeneration;
        var beforeCounter = rng.Counter;
        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(beforeCounter + 2, rng.Counter);
        Assert.Equal(2, result.SuccessCount);
        Assert.All(result.Cards, AssertGeneratedSword);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Aurora_generation_is_replaced_once_with_random_sword()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);

        var rng = Player.RunState.Rng.CombatCardGeneration;
        var beforeCounter = rng.Counter;

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Aurora, result.Request.OriginalKind);
        Assert.Equal(1, result.Request.OriginalCount);
        Assert.Equal(beforeCounter + 1, rng.Counter);
        Assert.Equal(1, result.SuccessCount);
        Assert.Single(result.Cards);
        AssertGeneratedSword(result.Cards.Single());
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand) + CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Crimson_generation_is_replaced_once_with_random_sword()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);

        var rng = Player.RunState.Rng.CombatCardGeneration;
        var beforeCounter = rng.Counter;

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Crimson, result.Request.OriginalKind);
        Assert.Equal(1, result.Request.OriginalCount);
        Assert.Equal(beforeCounter + 1, rng.Counter);
        Assert.Equal(1, result.SuccessCount);
        Assert.Single(result.Cards);
        AssertGeneratedSword(result.Cards.Single());
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand) + CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Original_aurora_request_still_triggers_twinbirth_after_replacement()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 1, Player.Creature);

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Aurora, result.Request.OriginalKind);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(2, CountAllSwords(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Sword_curtain_triggers_from_final_replacement_result()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);
        await ApplyPower<SwordCurtainPower>(Player.Creature, 1, Player.Creature);

        var blockBefore = Player.Creature.Block;
        var auroraSuccesses = 0;
        for (var i = 0; i < 8; i++)
        {
            var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
                new BlockingPlayerChoiceContext(),
                Player,
                1);
            auroraSuccesses += result.SuccessCountFor(SwordTokenKind.Aurora);
        }

        await WaitForIdle();

        Assert.Equal(blockBefore + auroraSuccesses, Player.Creature.Block);
        Assert.Equal(8, CountAllSwords(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Unnamed_card_40_triggers_from_final_replacement_result()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);
        await ApplyUnnamedCard40Power(chancePercent: 100, crimsonSwordCount: 1);

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Crimson, result.Request.OriginalKind);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1 + result.SuccessCountFor(SwordTokenKind.Crimson), CountAllSwords(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Full_hand_overflow_still_counts_as_success_and_triggers_twinbirth()
    {
        await ClearCombatPiles();
        await ApplyFormlessPower(1);
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 1, Player.Creature);

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<LinkedEdge>();

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.Cards.Count(card => card.EnteredHand));
        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Equal(2, CountAllSwords(PileType.Discard));

        await ExecuteRunnerAction();
    }

    private async Task<FormlessEmberlightPower> ApplyFormlessPower(int swordCount)
    {
        var power = await ApplyPower<FormlessEmberlightPower>(Player.Creature, 1, Player.Creature);
        Assert.NotNull(power);
        power.Configure(swordCount);
        return power;
    }

    private async Task<UnnamedCard40Power> ApplyUnnamedCard40Power(
        int chancePercent,
        int crimsonSwordCount)
    {
        var power = await ApplyPower<UnnamedCard40Power>(Player.Creature, 1, Player.Creature);
        Assert.NotNull(power);
        power.LayerChancePercents = [chancePercent];
        power.LayerCrimsonSwordCounts = [crimsonSwordCount];
        return power;
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task PlayWithEnergy(CardModel card, Creature target)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    private async Task ExecuteRunnerAction()
    {
        var hand = PileType.Hand.GetPile(Player);
        var actionCard = hand.Cards.OfType<LinkedEdge>().FirstOrDefault()
            ?? await AddToHand<LinkedEdge>();

        await PlayWithEnergy(actionCard, EnemyAt(0));
    }

    private int CountCards<TCard>(PileType pileType)
        where TCard : CardModel
    {
        return pileType.GetPile(Player).Cards.OfType<TCard>().Count();
    }

    private int CountAllSwords(PileType pileType)
    {
        return CountCards<AuroraSword>(pileType) + CountCards<CrimsonSword>(pileType);
    }

    private static void AssertGeneratedSword(SwordGeneratedCardResult result)
    {
        Assert.True(result.Success);
        Assert.True(result.Kind is SwordTokenKind.Aurora or SwordTokenKind.Crimson);
        Assert.True(result.Card is AuroraSword or CrimsonSword);
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
