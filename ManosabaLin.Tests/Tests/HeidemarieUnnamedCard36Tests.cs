using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard36Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-036");
    }

    [Fact]
    public async Task Playing_installs_stackable_mark_damage_bonus()
    {
        var baseCard = await AddToHand<UnnamedCard36>();
        await PlayWithEnergy(baseCard);

        Assert.Equal(
            baseCard.DynamicVars[UnnamedCard36.MarkDamageBonusKey].IntValue,
            Player.Creature.GetPower<UnnamedCard36Power>()?.Amount);

        var upgradedCard = await AddToHand<UnnamedCard36>();
        CardCmd.Upgrade(upgradedCard, CardPreviewStyle.None);
        await PlayWithEnergy(upgradedCard);

        Assert.Equal(3, Player.Creature.GetPower<UnnamedCard36Power>()?.Amount);
    }

    [Fact]
    public async Task Bonus_increases_owner_mark_damage()
    {
        await ApplyPower<MarkPower>(Player.Creature, 1, Player.Creature);
        await ApplyPower<UnnamedCard36Power>(Player.Creature, 2, Player.Creature);

        var attack = await AddToHand<AuroraSword>();
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;

        await PlayWithEnergy(attack, enemy);

        Assert.Equal(
            2m * attack.DynamicVars.Damage.BaseValue + MarkPower.Damage + 2m,
            DamageTaken(enemy, hpBefore));
        Assert.Null(Player.Creature.GetPower<MarkPower>());
    }

    [Fact]
    public async Task Bonus_does_not_affect_non_mark_damage_or_other_mark_owners()
    {
        var enemy = EnemyAt(0);
        await ApplyPower<UnnamedCard36Power>(enemy, 5, enemy);
        await ApplyPower<MarkPower>(Player.Creature, 1, Player.Creature);

        var attack = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;

        await PlayWithEnergy(attack, enemy);

        Assert.Equal(
            2m * attack.DynamicVars.Damage.BaseValue + MarkPower.Damage,
            DamageTaken(enemy, hpBefore));
    }

    private async Task PlayWithEnergy(CardModel card, Creature? target = null)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        if (target == null)
            await Play(card);
        else
            await Play(card, target);

        await WaitForIdle();
    }

    private static decimal DamageTaken(Creature creature, decimal hpBefore)
    {
        return hpBefore - creature.CurrentHp;
    }
}
