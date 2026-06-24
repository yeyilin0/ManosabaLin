using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieAuroraSwordTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Hiro>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-aurora-sword");
    }

    [Fact]
    public async Task Sword_generation_uses_production_aurora_sword_factory()
    {
        await ClearCombatPiles();

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        var generated = Assert.Single(result.Cards).Card;
        Assert.IsType<AuroraSword>(generated);
        Assert.IsAssignableFrom<IAuroraSwordCard>(generated);
        Assert.True(generated.HasComponent<SwordGraveComponent>());
        Assert.True(generated.HasComponent<LinkComponent>());
        Assert.Contains(generated, PileType.Hand.GetPile(Player).Cards);

        await PlayCleanupAction();
    }

    [Fact]
    public async Task Play_and_discard_damage_are_aurora_chain_powered_attacks()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);

        var unpoweredPlayDamage = await PlaySwordAndMeasureDamage(enemy);
        Assert.True(unpoweredPlayDamage > 0);

        var unpoweredDiscardDamage = await DiscardSwordAndMeasureDamage(enemy);
        Assert.True(unpoweredDiscardDamage > 0);

        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var poweredPlayDamage = await PlaySwordAndMeasureDamage(enemy);
        var poweredDiscardDamage = await DiscardSwordAndMeasureDamage(enemy);

        Assert.True(poweredPlayDamage > unpoweredPlayDamage);
        Assert.True(poweredDiscardDamage > unpoweredDiscardDamage);
    }

    [Fact]
    public async Task AutoPlay_null_target_uses_random_enemy_without_extra_discard_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var manualDamage = await PlaySwordAndMeasureDamage(enemy);

        var autoPlayed = await AddToHand<AuroraSword>();
        var hpBeforeAutoPlay = enemy.CurrentHp;
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            autoPlayed,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        var autoPlayDamage = hpBeforeAutoPlay - enemy.CurrentHp;
        Assert.Equal(manualDamage, autoPlayDamage);
        Assert.Same(PileType.Discard.GetPile(Player), autoPlayed.Pile);
    }

    [Fact]
    public async Task Link_cleanup_discards_other_aurora_sword_and_triggers_discard_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var singlePlayDamage = await PlaySwordAndMeasureDamage(enemy);

        var played = await AddToHand<AuroraSword>();
        var linked = await AddToHand<AuroraSword>();
        var hpBeforeLinkedPlay = enemy.CurrentHp;

        await PlayWithEnergy(played, enemy);

        var linkedPlayDamage = hpBeforeLinkedPlay - enemy.CurrentHp;
        Assert.True(linkedPlayDamage > singlePlayDamage);
        Assert.Same(PileType.Discard.GetPile(Player), linked.Pile);
    }

    [Fact]
    public async Task Non_discard_pile_move_does_not_trigger_discard_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var movedOnly = await AddToHand<AuroraSword>();
        var hpBeforeMove = enemy.CurrentHp;

        await CardPileCmd.Add(movedOnly, PileType.Discard);
        await WaitForIdle();

        Assert.Equal(hpBeforeMove, enemy.CurrentHp);
        Assert.Same(PileType.Discard.GetPile(Player), movedOnly.Pile);

        await PlayCleanupAction();
    }

    [Fact]
    public async Task No_hittable_enemy_makes_discard_and_autoplay_noop()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var noHitting = await ApplyPower<AuroraSwordNoHittingPower>(enemy, 1, Player.Creature);
        Assert.NotNull(noHitting);

        var discarded = await AddToHand<AuroraSword>();
        var autoPlayed = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), discarded);
        await WaitForIdle();

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            autoPlayed,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Equal(hpBefore, enemy.CurrentHp);
        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), autoPlayed.Pile);

        await PowerCmd.Remove(noHitting);
        await WaitForIdle();
        await PlayCleanupAction();
    }

    [Fact]
    public async Task Sword_grave_component_keeps_aurora_sword_in_discard_on_shuffle()
    {
        await ClearCombatPiles();

        var sword = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var normal = await CreateCardInPile<HiroAttack>(PileType.Discard);

        await CardPileCmd.Shuffle(new BlockingPlayerChoiceContext(), Player);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), sword.Pile);
        Assert.Same(PileType.Draw.GetPile(Player), normal.Pile);

        await PlayCleanupAction();
    }

    private async Task<int> PlaySwordAndMeasureDamage(Creature enemy)
    {
        var sword = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;
        await PlayWithEnergy(sword, enemy);
        return hpBefore - enemy.CurrentHp;
    }

    private async Task<int> DiscardSwordAndMeasureDamage(Creature enemy)
    {
        var sword = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), sword);
        await WaitForIdle();
        return hpBefore - enemy.CurrentHp;
    }

    private async Task PlayWithEnergy(CardModel card, Creature target)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    private async Task PlayCleanupAction()
    {
        await PlayWithEnergy(await AddToHand<HiroAttack>(), EnemyAt(0));
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

[RegisterPower]
public sealed class AuroraSwordNoHittingPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldAllowHitting(Creature creature)
    {
        return !ReferenceEquals(creature, Owner);
    }
}
