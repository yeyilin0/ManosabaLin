using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard35Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-035");
    }

    [Fact]
    public async Task Playing_without_owner_mark_does_not_apply_mark()
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard35>();
        var enemy = EnemyAt(0);

        await Play(card, enemy);

        Assert.Null(Player.Creature.GetPower<MarkPower>());
        Assert.Null(enemy.GetPower<MarkPower>());
    }

    [Fact]
    public async Task Playing_with_owner_mark_adds_extra_mark_to_owner()
    {
        const int initialMark = 2;
        await ApplyPower<MarkPower>(Player.Creature, initialMark, Player.Creature);
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard35>();
        var enemy = EnemyAt(0);
        var expectedMark = initialMark + card.DynamicVars[UnnamedCard35.MarkVar].IntValue;

        await Play(card, enemy);

        Assert.Equal(expectedMark, Player.Creature.GetPower<MarkPower>()?.Amount);
        Assert.Null(enemy.GetPower<MarkPower>());
    }

    [Fact]
    public async Task Enemy_mark_does_not_trigger_owner_mark_gain()
    {
        const int enemyMark = 2;
        var enemy = EnemyAt(0);
        await ApplyPower<MarkPower>(enemy, enemyMark, Player.Creature);
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard35>();

        await Play(card, enemy);

        Assert.Null(Player.Creature.GetPower<MarkPower>());
        Assert.Equal(enemyMark, enemy.GetPower<MarkPower>()?.Amount);
    }

    [Fact]
    public async Task Play_wrapper_with_no_target_completes_without_target_state()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await Play(await AddToHand<UnnamedCard35>(), EnemyAt(0));

        const int initialMark = 1;
        await ApplyPower<MarkPower>(Player.Creature, initialMark, Player.Creature);
        var card = await AddToHand<UnnamedCard35>();
        var enemy = EnemyAt(0);
        var expectedMark = initialMark + card.DynamicVars[UnnamedCard35.MarkVar].IntValue;
        var resources = new ResourceInfo
        {
            EnergySpent = 0,
            EnergyValue = card.EnergyCost.GetAmountToSpend(),
            StarsSpent = 0,
            StarValue = 0
        };

        await card.OnPlayWrapper(
            new BlockingPlayerChoiceContext(),
            null,
            isAutoPlay: true,
            resources,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Equal(expectedMark, Player.Creature.GetPower<MarkPower>()?.Amount);
        Assert.Null(enemy.GetPower<MarkPower>());
        Assert.DoesNotContain(card, PileType.Hand.GetPile(Player).Cards);
    }
}
