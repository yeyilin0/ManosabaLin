using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieCrimsonSwordTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-crimson-sword");
    }

    [Fact]
    public async Task Production_factory_generates_real_crimson_sword()
    {
        await ClearCombatPiles();

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);

        var generated = Assert.Single(result.Cards).Card;
        Assert.IsType<CrimsonSword>(generated);
        Assert.Same(PileType.Hand.GetPile(Player), generated.Pile);
        Assert.True(generated is ICrimsonSwordCard);
        Assert.True(generated.HasComponent<SwordGraveComponent>());
        Assert.True(generated.HasComponent<LinkComponent>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Aurora_chain_increases_crimson_sword_attack_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var baseline = await AddToHand<CrimsonSword>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        var beforeBaseline = enemy.CurrentHp;
        await Play(baseline, enemy);
        var baselineDamage = beforeBaseline - enemy.CurrentHp;

        await ApplyPower<AuroraChainPower>(Player.Creature, 2, Player.Creature);
        var boosted = await AddToHand<CrimsonSword>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();

        var beforeBoosted = enemy.CurrentHp;
        await Play(boosted, enemy);
        var boostedDamage = beforeBoosted - enemy.CurrentHp;

        Assert.True(baselineDamage > 0);
        Assert.True(boostedDamage > baselineDamage);
    }

    [Fact]
    public async Task Hand_discard_grants_aurora_chain_without_discard_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var card = await AddToHand<CrimsonSword>();
        var expectedGain = card.DynamicVars["AuroraChain"].BaseValue;
        var hpBeforeDiscard = enemy.CurrentHp;

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        var power = Player.Creature.GetPower<AuroraChainPower>();
        Assert.NotNull(power);
        Assert.Equal(expectedGain, power.Amount);
        Assert.Equal(hpBeforeDiscard, enemy.CurrentHp);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Play_and_autoplay_result_pile_moves_do_not_grant_aurora_chain()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var manual = await AddToHand<CrimsonSword>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(manual, enemy);

        Assert.Null(Player.Creature.GetPower<AuroraChainPower>());

        var autoplay = await AddToHand<CrimsonSword>();
        var hpBeforeAutoplay = enemy.CurrentHp;

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            autoplay,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.True(enemy.CurrentHp < hpBeforeAutoplay);
        Assert.Null(Player.Creature.GetPower<AuroraChainPower>());
    }

    [Fact]
    public async Task Link_cleanup_discard_grants_aurora_chain_for_other_linked_card_only()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var played = await AddToHand<CrimsonSword>();
        var linked = await AddToHand<CrimsonSword>();
        var expectedGain = linked.DynamicVars["AuroraChain"].BaseValue;

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(played, enemy);

        var power = Player.Creature.GetPower<AuroraChainPower>();
        Assert.NotNull(power);
        Assert.Equal(expectedGain, power.Amount);
        Assert.Same(PileType.Discard.GetPile(Player), linked.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), played.Pile);
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

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddToHand<CrimsonSword>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
    }
}
