using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard39Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-039");
    }

    [Fact]
    public async Task Deals_attack_damage_to_all_enemies_and_gains_mark_per_unblocked_hit()
    {
        var card = await AddToHand<UnnamedCard39>();
        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(card);

        Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(enemies[0], hpBefore[0]));
        Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(enemies[1], hpBefore[1]));
        Assert.Equal(2 * card.DynamicVars[UnnamedCard39.MarkVar].IntValue, Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task Blocked_damage_does_not_grant_mark_for_that_enemy()
    {
        var card = await AddToHand<UnnamedCard39>();
        var blockedEnemy = EnemyAt(0);
        var unblockedEnemy = EnemyAt(1);
        await CreatureCmd.GainBlock(blockedEnemy, card.DynamicVars.Damage.BaseValue, ValueProp.Unpowered, null);
        await WaitForIdle();

        var hpBefore = HpSnapshot(Enemies());

        await PlayWithEnergy(card);

        Assert.Equal(0m, DamageTaken(blockedEnemy, hpBefore[0]));
        Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(unblockedEnemy, hpBefore[1]));
        Assert.Equal(card.DynamicVars[UnnamedCard39.MarkVar].IntValue, Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task Upgrade_increases_damage_and_mark_gain()
    {
        var card = await AddToHand<UnnamedCard39>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(card);

        Assert.Equal(2m, DamageTaken(enemies[0], hpBefore[0]));
        Assert.Equal(2m, DamageTaken(enemies[1], hpBefore[1]));
        Assert.Equal(4, Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task Existing_mark_triggers_once_for_the_aoe_attack_before_rewards()
    {
        const int initialMark = 3;
        await ApplyPower<MarkPower>(Player.Creature, initialMark, Player.Creature);
        var card = await AddToHand<UnnamedCard39>();

        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(card);

        Assert.Equal(2m * card.DynamicVars.Damage.BaseValue + MarkPower.Damage, TotalDamageTaken(enemies, hpBefore));
        Assert.Equal(
            initialMark - 1 + 2 * card.DynamicVars[UnnamedCard39.MarkVar].IntValue,
            Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task No_alive_enemies_is_safe_noop()
    {
        const int initialMark = 2;
        await PlayWithEnergy(await AddToHand<UnnamedCard34>());
        var setupMark = Player.Creature.GetPower<MarkPower>();
        if (setupMark != null)
            await PowerCmd.Remove(setupMark);
        await WaitForIdle();

        await ApplyPower<MarkPower>(Player.Creature, initialMark, Player.Creature);

        var card = await AddToHand<UnnamedCard39>();
        var enemies = Enemies();
        foreach (var enemy in enemies)
            await CreatureCmd.SetCurrentHp(enemy, 0m);
        await WaitForIdle();

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.All(enemies, enemy => Assert.False(enemy.IsAlive));
        Assert.Equal(initialMark, Player.Creature.GetPower<MarkPower>()?.Amount);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
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
}
