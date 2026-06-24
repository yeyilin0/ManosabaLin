using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieBattlemarkBondTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-battlemark-bond");
    }

    [Fact]
    public async Task Play_installs_battlemark_bond_power()
    {
        var card = await AddToHand<BattlemarkBond>();

        await Play(card);

        Assert.NotNull(Player.Creature.GetPower<BattlemarkBondPower>());
    }

    [Fact]
    public async Task Single_player_owner_attack_gains_mark()
    {
        await ApplyPower<BattlemarkBondPower>(Player.Creature, 1, Player.Creature);
        var attack = await AddToHand<StrikeIronclad>();

        await Play(attack, EnemyAt(0));

        Assert.True(Player.Creature.GetPower<MarkPower>()?.Amount > 0);
    }

    [Fact]
    public async Task Upgraded_card_has_innate_keyword()
    {
        var card = await AddToHand<BattlemarkBond>();

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Contains(CardKeyword.Innate, card.Keywords);
        await Play(card);
    }
}
