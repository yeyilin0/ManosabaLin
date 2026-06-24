using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
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

public sealed class HeidemarieLiberatedAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-liberated-aurora");
    }

    [Fact]
    public async Task No_aurora_swords_deals_base_attack_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var card = await AddToHand<LiberatedAurora>();
        var hpBefore = enemy.CurrentHp;

        await PlayWithEnergy(card, enemy);

        Assert.Equal(card.DynamicVars.Damage.BaseValue, DamageTaken(enemy, hpBefore));
    }

    [Fact]
    public async Task Consumes_aurora_swords_across_combat_piles_and_adds_damage()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var card = await AddToHand<LiberatedAurora>();
        var handSword = await AddToHand<AuroraSword>();
        var drawSword = await CreateCardInPile<AuroraSword>(PileType.Draw);
        var discardSword = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var hpBefore = enemy.CurrentHp;

        await PlayWithEnergy(card, enemy);

        Assert.Equal(card.DynamicVars.Damage.BaseValue + 3m, DamageTaken(enemy, hpBefore));
        Assert.All(new[] { handSword, drawSword, discardSword },
            sword => Assert.Same(PileType.Exhaust.GetPile(Player), sword.Pile));
    }

    [Fact]
    public async Task External_exhaust_generates_condensed_auroras()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LiberatedAurora>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Same(PileType.Exhaust.GetPile(Player), card.Pile);
        Assert.Equal(
            card.DynamicVars[LiberatedAurora.CondensedAurorasKey].IntValue,
            PileType.Hand.GetPile(Player).Cards.Count(c => c is CondensedAurora));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task No_target_autoplay_is_safe_and_does_not_consume_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<LiberatedAurora>();
        var drawSword = await CreateCardInPile<AuroraSword>(PileType.Draw);
        var noHitting = await ApplyPower<AuroraSwordNoHittingPower>(EnemyAt(0), 1, Player.Creature);
        Assert.NotNull(noHitting);
        await WaitForIdle();

        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Same(PileType.Draw.GetPile(Player), drawSword.Pile);

        await PowerCmd.Remove(noHitting);
        await WaitForIdle();
        await ExecuteRunnerAction();
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
        await WaitForIdle();
    }

    private async Task PlayWithEnergy(CardModel card, Creature target)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
        await WaitForIdle();
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
