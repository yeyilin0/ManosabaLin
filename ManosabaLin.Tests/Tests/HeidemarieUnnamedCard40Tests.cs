using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
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

public sealed class HeidemarieUnnamedCard40Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-unnamed-card-40");
    }

    [Fact]
    public async Task Play_installs_power_layer_with_base_values()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard40>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<UnnamedCard40Power>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Equal([UnnamedCard40Power.BaseChancePercent], power.LayerChancePercents);
        Assert.Equal([UnnamedCard40Power.BaseCrimsonSwordCount], power.LayerCrimsonSwordCounts);
    }

    [Fact]
    public async Task Upgraded_card_installs_power_layer_with_upgraded_values()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard40>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<UnnamedCard40Power>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Equal([2], power.LayerChancePercents);
        Assert.Equal([2], power.LayerCrimsonSwordCounts);
    }

    [Fact]
    public async Task Crimson_generation_can_add_bonus_without_self_recursing()
    {
        await ClearCombatPiles();
        await ApplyUnnamedCard40Power(chancePercent: 100, crimsonSwordCount: 1);

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(2, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Aurora_generation_does_not_trigger_bonus_crimson_generation()
    {
        await ClearCombatPiles();
        await ApplyUnnamedCard40Power(chancePercent: 100, crimsonSwordCount: 1);

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(0, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Chance_roll_uses_combat_card_generation_rng()
    {
        await ClearCombatPiles();
        await ApplyUnnamedCard40Power(chancePercent: 1, crimsonSwordCount: 1);

        var rng = Player.RunState.Rng.CombatCardGeneration;
        var beforeCounter = rng.Counter;
        await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(beforeCounter + 1, rng.Counter);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Other_layers_can_trigger_from_bonus_crimson_generation()
    {
        await ClearCombatPiles();
        await ApplyUnnamedCard40Power(chancePercent: 100, crimsonSwordCount: 1, layers: 2);

        await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(5, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Full_hand_success_still_triggers_bonus_and_uses_helper_overflow()
    {
        await ClearCombatPiles();
        await ApplyUnnamedCard40Power(chancePercent: 100, crimsonSwordCount: 1);

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<LinkedEdge>();

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.EnteredHandCountFor(SwordTokenKind.Crimson));
        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Equal(2, CountCards<CrimsonSword>(PileType.Discard));

        await ExecuteRunnerAction();
    }

    private async Task<UnnamedCard40Power> ApplyUnnamedCard40Power(
        int chancePercent,
        int crimsonSwordCount,
        int layers = 1)
    {
        var power = await ApplyPower<UnnamedCard40Power>(Player.Creature, layers, Player.Creature);
        Assert.NotNull(power);
        power.LayerChancePercents = Enumerable.Repeat(chancePercent, layers).ToArray();
        power.LayerCrimsonSwordCounts = Enumerable.Repeat(crimsonSwordCount, layers).ToArray();
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
