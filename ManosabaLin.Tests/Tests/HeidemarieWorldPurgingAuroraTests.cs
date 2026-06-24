using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Hiro.Cards;
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

public sealed class HeidemarieWorldPurgingAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-world-purging-aurora");
    }

    [Fact]
    public async Task No_liberated_aurora_deals_base_aoe_and_does_not_consume_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<WorldPurgingAurora>();
        var drawSword = await CreateCardInPile<AuroraSword>(PileType.Draw);
        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(card);

        Assert.All(enemies, enemy => Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(enemy, hpBefore[Array.IndexOf(enemies, enemy)])));
        Assert.Same(PileType.Draw.GetPile(Player), drawSword.Pile);
    }

    [Fact]
    public async Task Liberated_auroras_exhaust_all_and_sword_count_boosts_each_enemy()
    {
        await ClearCombatPiles();

        var card = await AddToHand<WorldPurgingAurora>();
        var firstLiberated = await AddToHand<LiberatedAurora>();
        var secondLiberated = await AddToHand<LiberatedAurora>();
        var handSword = await AddToHand<AuroraSword>();
        var drawSword = await CreateCardInPile<AuroraSword>(PileType.Draw);
        var discardSword = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var enemies = Enemies();
        var hpBefore = HpSnapshot(enemies);

        await PlayWithEnergy(card);

        var expectedDamage = card.DynamicVars.Damage.BaseValue + 3m;
        Assert.Equal(expectedDamage, DamageTaken(enemies[0], hpBefore[0]));
        Assert.Equal(expectedDamage, DamageTaken(enemies[1], hpBefore[1]));
        Assert.All(new CardModel[] { firstLiberated, secondLiberated, handSword, drawSword, discardSword },
            exhausted => Assert.Same(PileType.Exhaust.GetPile(Player), exhausted.Pile));
        Assert.Equal(2, PileType.Hand.GetPile(Player).Cards.Count(c => c is CondensedAurora));
    }

    [Fact]
    public async Task No_alive_enemies_is_safe()
    {
        await ClearCombatPiles();
        await ExecuteRunnerAction();

        var card = await AddToHand<WorldPurgingAurora>();
        var liberated = await AddToHand<LiberatedAurora>();
        await CreateCardInPile<AuroraSword>(PileType.Draw);
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
        Assert.NotNull(liberated.Pile);
    }

    [Fact]
    public async Task Upgrade_lowers_energy_cost()
    {
        await ClearCombatPiles();

        var card = await AddToHand<WorldPurgingAurora>();
        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));

        await ExecuteRunnerAction();
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddToHand<HiroDefend>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard);
        await WaitForIdle();
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

    private static decimal DamageTaken(Creature creature, decimal hpBefore)
    {
        return hpBefore - creature.CurrentHp;
    }

    private async Task<TCard> CreateCardInPile<TCard>(PileType pileType)
        where TCard : CardModel
    {
        var card = Combat.CreateCard<TCard>(Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, pileType, Player);
        await WaitFor(
            () => pileType.GetPile(Player).Cards.Contains(card),
            $"{typeof(TCard).Name} did not appear in {pileType}.");
        return card;
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
