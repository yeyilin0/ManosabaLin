using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieShatteredAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-shattered-aurora");
    }

    [Fact]
    public async Task Play_installs_shattered_aurora_power()
    {
        await ClearCombatPiles();

        var card = await AddToHand<ShatteredAurora>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<ShatteredAuroraPower>();
        Assert.NotNull(power);
        Assert.Equal(ShatteredAuroraPower.BaseDiscardThreshold, power.DiscardThreshold);
        Assert.Equal(ShatteredAuroraPower.BaseEnergyGain, power.EnergyGain);
        Assert.Equal(PowerStackType.Single, power.StackType);
    }

    [Fact]
    public async Task Aurora_sword_discards_accumulate_and_gain_energy_at_threshold()
    {
        await ClearCombatPiles();
        var power = await PlayShatteredAuroraPower();

        await PlayerCmd.SetEnergy(0, Player);
        await WaitForIdle();

        var first = await AddToHand<AuroraSword>();
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), first);
        await WaitForIdle();

        Assert.Equal(1, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(0, CurrentEnergy);

        var second = await AddToHand<AuroraSword>();
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), second);
        await WaitForIdle();

        Assert.Equal(0, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(ShatteredAuroraPower.BaseEnergyGain, CurrentEnergy);
    }

    [Fact]
    public async Task Extra_discards_preserve_remainder()
    {
        await ClearCombatPiles();
        var power = await PlayShatteredAuroraPower();

        await PlayerCmd.SetEnergy(0, Player);
        await WaitForIdle();

        var swords = new[]
        {
            await AddToHand<AuroraSword>(),
            await AddToHand<AuroraSword>(),
            await AddToHand<AuroraSword>()
        };

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), swords);
        await WaitForIdle();

        Assert.Equal(1, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(ShatteredAuroraPower.BaseEnergyGain, CurrentEnergy);
    }

    [Fact]
    public async Task Non_aurora_discards_do_not_count()
    {
        await ClearCombatPiles();
        var power = await PlayShatteredAuroraPower();

        await PlayerCmd.SetEnergy(0, Player);
        await WaitForIdle();

        var nonAurora = await AddToHand<HiroDefend>();
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), nonAurora);
        await WaitForIdle();

        Assert.Equal(0, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(0, CurrentEnergy);
    }

    [Fact]
    public async Task Playing_twice_keeps_one_layer_and_does_not_double_energy()
    {
        await ClearCombatPiles();

        var firstCard = await AddToHand<ShatteredAurora>();
        var secondCard = await AddToHand<ShatteredAurora>();

        await PlayWithEnergy(firstCard);
        await PlayWithEnergy(secondCard);

        var powers = Player.Creature.GetPowerInstances(ModelDb.Power<ShatteredAuroraPower>().Id).ToArray();
        var power = Assert.Single(powers);
        Assert.Equal(1, power.Amount);

        await PlayerCmd.SetEnergy(0, Player);
        await WaitForIdle();

        await CardCmd.Discard(
            new BlockingPlayerChoiceContext(),
            [await AddToHand<AuroraSword>(), await AddToHand<AuroraSword>()]);
        await WaitForIdle();

        Assert.Equal(ShatteredAuroraPower.BaseEnergyGain, CurrentEnergy);
    }

    [Fact]
    public async Task Upgraded_card_lowers_trigger_threshold()
    {
        await ClearCombatPiles();

        var card = await AddToHand<ShatteredAurora>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<ShatteredAuroraPower>();
        Assert.NotNull(power);
        Assert.Equal(1, power.DiscardThreshold);

        await PlayerCmd.SetEnergy(0, Player);
        await WaitForIdle();

        var sword = await AddToHand<AuroraSword>();
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), sword);
        await WaitForIdle();

        Assert.Equal(0, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(ShatteredAuroraPower.BaseEnergyGain, CurrentEnergy);
    }

    [Fact]
    public async Task Link_discards_multiple_aurora_swords_one_by_one()
    {
        await ClearCombatPiles();
        var power = await PlayShatteredAuroraPower();

        var played = await AddToHand<LinkedEdge>();
        var firstLinkedSword = await AddToHand<AuroraSword>();
        var secondLinkedSword = await AddToHand<AuroraSword>();
        var nonLinked = await AddToHand<HiroDefend>();

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        var energyBeforePlay = CurrentEnergy;
        var linkedEdgeCost = played.EnergyCost.GetWithModifiers(CostModifiers.None);

        await Play(played, EnemyAt(0));

        Assert.Same(PileType.Discard.GetPile(Player), firstLinkedSword.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), secondLinkedSword.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), nonLinked.Pile);
        Assert.Equal(0, power.DiscardedAuroraSwordRemainder);
        Assert.Equal(energyBeforePlay - linkedEdgeCost + ShatteredAuroraPower.BaseEnergyGain, CurrentEnergy);
    }

    private async Task PlayWithEnergy(CardModel card, Creature? target = null)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    private async Task<ShatteredAuroraPower> PlayShatteredAuroraPower()
    {
        var card = await AddToHand<ShatteredAurora>();
        await PlayWithEnergy(card);

        return Player.Creature.GetPower<ShatteredAuroraPower>()
            ?? throw new InvalidOperationException("Shattered Aurora did not install its power.");
    }

    private int CurrentEnergy => Player.PlayerCombatState!.Energy;

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
