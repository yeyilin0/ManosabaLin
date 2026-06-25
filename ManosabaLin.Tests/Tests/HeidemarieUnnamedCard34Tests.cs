using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard34Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-034");
    }

    [Fact]
    public async Task Playing_gains_block_from_card_block_var()
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard34>();
        var blockBefore = Player.Creature.Block;
        var expectedBlockGain = (int)card.DynamicVars.Block.BaseValue;

        await Play(card);

        Assert.Equal(blockBefore + expectedBlockGain, Player.Creature.Block);
    }

    [Fact]
    public async Task Playing_applies_mark_to_owner_side()
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard34>();
        var expectedMark = (int)card.DynamicVars[UnnamedCard34.MarkVar].BaseValue;
        var enemy = EnemyAt(0);

        await Play(card);

        Assert.Equal(expectedMark, Player.Creature.GetPower<MarkPower>()?.Amount);
        Assert.Null(enemy.GetPower<MarkPower>());
    }

    [Fact]
    public async Task Playing_without_enemy_target_completes()
    {
        await PlayerCmd.SetEnergy(10, Player);
        var card = await AddToHand<UnnamedCard34>();

        await Play(card);

        Assert.DoesNotContain(card, PileType.Hand.GetPile(Player).Cards);
        Assert.NotNull(Player.Creature.GetPower<MarkPower>());
    }
}
