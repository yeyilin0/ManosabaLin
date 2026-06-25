using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieLinkedEdgeTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-linked-edge");
    }

    [Fact]
    public async Task Deals_base_damage_without_other_linked_cards()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var linkedEdge = await AddToHand<LinkedEdge>();
        var hpBefore = enemy.CurrentHp;

        await PlayerCmd.SetEnergy(10, Player);
        await Play(linkedEdge, enemy);

        Assert.Equal(linkedEdge.DynamicVars.Damage.BaseValue, hpBefore - enemy.CurrentHp);
    }

    [Fact]
    public async Task Deals_more_damage_for_other_linked_cards_in_hand()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var linkedEdge = await AddToHand<LinkedEdge>();
        var linkedCard = await AddToHand<HiroDefend>();
        var unlinkedCard = await AddToHand<HiroDefend>();
        linkedCard.TryAddComponent(new LinkComponent());
        var expectedDamage = linkedEdge.DynamicVars.Damage.BaseValue
            + linkedEdge.DynamicVars[LinkedEdge.PerLinkDamageKey].BaseValue;
        var hpBefore = enemy.CurrentHp;

        await PlayerCmd.SetEnergy(10, Player);
        await Play(linkedEdge, enemy);

        Assert.Equal(expectedDamage, hpBefore - enemy.CurrentHp);
        Assert.Same(PileType.Hand.GetPile(Player), unlinkedCard.Pile);
    }

    [Fact]
    public async Task Manual_play_discards_old_linked_cards_after_resolving()
    {
        await ClearCombatPiles();

        var linkedEdge = await AddToHand<LinkedEdge>();
        var linkedAttack = await AddToHand<HiroAttack>();
        var linkedSkill = await AddToHand<HiroDefend>();
        var unlinkedSkill = await AddToHand<HiroDefend>();
        linkedAttack.TryAddComponent(new LinkComponent());
        linkedSkill.TryAddComponent(new LinkComponent());

        await PlayerCmd.SetEnergy(10, Player);
        await Play(linkedEdge, EnemyAt(0));

        Assert.Same(PileType.Discard.GetPile(Player), linkedAttack.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), linkedSkill.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unlinkedSkill.Pile);
    }

    [Fact]
    public async Task Upgrade_increases_damage_added_per_linked_card()
    {
        await ClearCombatPiles();

        var normalDamage = await PlayWithOneLinkedCard(upgraded: false);
        await ClearCombatPiles();
        var upgradedDamage = await PlayWithOneLinkedCard(upgraded: true);

        Assert.True(upgradedDamage > normalDamage);
    }

    private async Task<decimal> PlayWithOneLinkedCard(bool upgraded)
    {
        var enemy = EnemyAt(0);
        var linkedEdge = await AddToHand<LinkedEdge>();
        var linkedCard = await AddToHand<HiroDefend>();
        linkedCard.TryAddComponent(new LinkComponent());

        if (upgraded)
            CardCmd.Upgrade(linkedEdge, CardPreviewStyle.None);

        var hpBefore = enemy.CurrentHp;
        await PlayerCmd.SetEnergy(10, Player);
        await Play(linkedEdge, enemy);
        return hpBefore - enemy.CurrentHp;
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
