using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieSwordlightTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-swordlight");
    }

    [Fact]
    public async Task Discard_pile_swordlight_doubles_aurora_sword_damage_after_aurora_chain()
    {
        await ClearCombatPiles();

        await PlayWithEnergy(await AddToHand<Swordlight>());
        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var damage = await PlayAuroraSwordAndMeasureDamage();

        Assert.Equal(8, damage);
    }

    [Fact]
    public async Task Unplayed_swordlight_discarded_from_hand_doubles_aurora_sword_damage()
    {
        await ClearCombatPiles();

        var swordlight = await AddToHand<Swordlight>();
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), swordlight);
        await WaitForIdle();
        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var damage = await PlayAuroraSwordAndMeasureDamage();

        Assert.Equal(8, damage);
    }

    [Fact]
    public async Task Unplayed_swordlight_directly_in_discard_doubles_only_until_it_leaves_discard()
    {
        await ClearCombatPiles();

        var swordlight = await CreateCardInPile<Swordlight>(PileType.Discard);
        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var doubledDamage = await PlayAuroraSwordAndMeasureDamage();
        await CardPileCmd.Add(swordlight, PileType.Draw);
        await WaitForIdle();
        var normalDamage = await PlayAuroraSwordAndMeasureDamage();

        Assert.Equal(8, doubledDamage);
        Assert.Equal(4, normalDamage);
    }

    [Fact]
    public async Task Swordlight_outside_discard_pile_does_not_double_aurora_sword_damage()
    {
        await ClearCombatPiles();

        var swordlight = await AddToHand<Swordlight>();
        await PlayWithEnergy(swordlight);
        await CardPileCmd.Add(swordlight, PileType.Draw);
        await WaitForIdle();
        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var damage = await PlayAuroraSwordAndMeasureDamage();

        Assert.Equal(4, damage);
    }

    [Fact]
    public async Task Owner_turn_start_moves_discard_pile_swordlight_to_draw_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Swordlight>();
        await PlayWithEnergy(card);

        await TriggerPlayerTurnStartBeforeDraw();

        Assert.Same(PileType.Draw.GetPile(Player), card.Pile);
        Assert.DoesNotContain(card, PileType.Discard.GetPile(Player).Cards);
        Assert.DoesNotContain(card, PileType.Hand.GetPile(Player).Cards);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_swordlight_moves_to_draw_then_bounces_to_hand()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Swordlight>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        AssertBounce(card, 1m);
        await PlayWithEnergy(card);

        await TriggerPlayerTurnStartBeforeDraw();

        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);
        Assert.Null(GetBounce(card));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_swordlight_still_moves_to_draw_when_bounce_cannot_enter_full_hand()
    {
        await ClearCombatPiles();

        var card = await AddToHand<Swordlight>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        await PlayWithEnergy(card);
        await FillHandToMax();

        await TriggerPlayerTurnStartBeforeDraw();

        Assert.Same(PileType.Draw.GetPile(Player), card.Pile);
        AssertBounce(card, 1m);

        await ExecuteRunnerAction();
    }

    private async Task<int> PlayAuroraSwordAndMeasureDamage()
    {
        var enemy = EnemyAt(0);
        var sword = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;

        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(sword, enemy);

        return hpBefore - enemy.CurrentHp;
    }

    private async Task<TCard> CreateCardInPile<TCard>(PileType pileType) where TCard : CardModel
    {
        var card = Combat.CreateCard<TCard>(Player);
        await CardPileCmd.AddGeneratedCardToCombat(card, pileType, Player);
        await WaitFor(() => pileType.GetPile(Player).Cards.Contains(card), $"Expected {typeof(TCard).Name} in {pileType}");

        return card;
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task TriggerPlayerTurnStartBeforeDraw()
    {
        await Hook.BeforeSideTurnStart(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();
    }

    private async Task FillHandToMax()
    {
        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();
    }

    private static void AssertBounce(CardModel card, decimal expectedAmount)
    {
        var bounce = GetBounce(card);
        Assert.NotNull(bounce);
        Assert.Equal(expectedAmount, bounce.Amount);
    }

    private static BounceComponent? GetBounce(CardModel card)
    {
        return Assert.IsAssignableFrom<IComponentsCardModel>(card).GetComponent<BounceComponent>();
    }

    private async Task ExecuteRunnerAction()
    {
        var hand = PileType.Hand.GetPile(Player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            await CardPileCmd.RemoveFromCombat(hand.Cards[0], skipVisuals: true);
            await WaitForIdle();
        }

        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
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
