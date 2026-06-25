using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieThousandAuroraShatterstrikeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-thousand-aurora-shatterstrike");
    }

    [Fact]
    public async Task No_aurora_swords_deals_only_base_damage_to_chosen_target()
    {
        await ClearCombatPiles();

        var target = EnemyAt(0);
        var otherEnemy = EnemyAt(1);
        var card = await AddToHand<ThousandAuroraShatterstrike>();
        var targetHpBefore = target.CurrentHp;
        var otherHpBefore = otherEnemy.CurrentHp;

        await PlayWithEnergy(card, target);

        Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(target, targetHpBefore));
        Assert.Equal(0m, DamageTaken(otherEnemy, otherHpBefore));
    }

    [Fact]
    public async Task Aurora_swords_in_hand_create_one_extra_segment_each()
    {
        await ClearCombatPiles();

        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);
        var expectedDamage = card.DynamicVars.Damage.BaseValue
            + 2m * card.DynamicVars[ThousandAuroraShatterstrike.ExtraDamageKey].BaseValue;

        await PlayWithEnergy(card, EnemyAt(0));

        Assert.Equal(expectedDamage, TotalDamageTaken(enemies, hpBefore));
    }

    [Fact]
    public async Task Extra_segments_are_attack_damage_observed_by_mark()
    {
        await ClearCombatPiles();
        await ApplyPower<MarkPower>(Player.Creature, 5, Player.Creature);

        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);
        var expectedAttackSegments = 3m;
        var expectedDamage = card.DynamicVars.Damage.BaseValue
            + 2m * card.DynamicVars[ThousandAuroraShatterstrike.ExtraDamageKey].BaseValue
            + expectedAttackSegments * MarkPower.Damage;

        await PlayWithEnergy(card, EnemyAt(0));

        Assert.Equal(expectedDamage, TotalDamageTaken(enemies, hpBefore));
        Assert.Equal(2, Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task Upgrade_increases_extra_damage_only()
    {
        await ClearCombatPiles();

        var normalDamage = await PlayWithOneAuroraSword(upgraded: false);
        await ClearCombatPiles();

        var upgradedCard = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        CardCmd.Upgrade(upgradedCard, CardPreviewStyle.None);

        Assert.Equal(1m, upgradedCard.DynamicVars.Damage.BaseValue);
        Assert.Equal(2m, upgradedCard.DynamicVars[ThousandAuroraShatterstrike.ExtraDamageKey].BaseValue);

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(upgradedCard, EnemyAt(0));

        var upgradedDamage = TotalDamageTaken(enemies, hpBefore);
        Assert.Equal(1m, upgradedDamage - normalDamage);
    }

    [Fact]
    public async Task Extra_segments_reselect_after_chosen_enemy_dies()
    {
        await ClearCombatPiles();

        var target = EnemyAt(0);
        var remainingEnemy = EnemyAt(1);
        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();
        await CreatureCmd.SetCurrentHp(target, card.DynamicVars.Damage.BaseValue);
        await WaitForIdle();

        var remainingHpBefore = remainingEnemy.CurrentHp;

        await PlayWithEnergy(card, target);

        Assert.False(target.IsAlive);
        Assert.Equal(
            2m * card.DynamicVars[ThousandAuroraShatterstrike.ExtraDamageKey].BaseValue,
            DamageTaken(remainingEnemy, remainingHpBefore));
    }

    [Fact]
    public async Task No_hittable_enemies_makes_extra_segments_noop()
    {
        await ClearCombatPiles();

        var noHittingPowers = new List<ThousandAuroraNoHittingPower>();
        foreach (var enemy in Enemies())
        {
            var noHitting = await ApplyPower<ThousandAuroraNoHittingPower>(enemy, 1, Player.Creature);
            Assert.NotNull(noHitting);
            noHittingPowers.Add(noHitting);
        }

        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Equal(0m, TotalDamageTaken(enemies, hpBefore));

        foreach (var noHitting in noHittingPowers)
            await PowerCmd.Remove(noHitting);
        await WaitForIdle();
        await PlayWithEnergy(await AddToHand<AuroraSword>(), EnemyAt(0));
    }

    [Fact]
    public async Task Seeded_random_extra_targets_are_reproducible()
    {
        await ClearCombatPiles();

        var firstEnemy = EnemyAt(0);
        var secondEnemy = EnemyAt(1);
        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();
        await AddToHand<AuroraSword>();

        var firstHpBefore = firstEnemy.CurrentHp;
        var secondHpBefore = secondEnemy.CurrentHp;

        await PlayWithEnergy(card, firstEnemy);

        Assert.Equal(3m, DamageTaken(firstEnemy, firstHpBefore));
        Assert.Equal(1m, DamageTaken(secondEnemy, secondHpBefore));
    }

    private async Task<decimal> PlayWithOneAuroraSword(bool upgraded)
    {
        var card = await AddToHand<ThousandAuroraShatterstrike>();
        await AddToHand<AuroraSword>();
        if (upgraded)
            CardCmd.Upgrade(card, CardPreviewStyle.None);

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);
        await PlayWithEnergy(card, EnemyAt(0));
        return TotalDamageTaken(enemies, hpBefore);
    }

    private async Task PlayWithEnergy(CardModel card, Creature target)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
        await WaitForIdle();
    }

    private Creature[] Enemies()
    {
        return [EnemyAt(0), EnemyAt(1)];
    }

    private static decimal[] HpSnapshot(Creature[] creatures)
    {
        return creatures.Select(creature => (decimal)creature.CurrentHp).ToArray();
    }

    private static decimal TotalDamageTaken(Creature[] creatures, decimal[] hpBefore)
    {
        return creatures.Select((creature, index) => DamageTaken(creature, hpBefore[index])).Sum();
    }

    private static decimal DamageTaken(Creature creature, decimal hpBefore)
    {
        return hpBefore - creature.CurrentHp;
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
public sealed class ThousandAuroraNoHittingPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldAllowHitting(Creature creature)
    {
        return !ReferenceEquals(creature, Owner);
    }
}
