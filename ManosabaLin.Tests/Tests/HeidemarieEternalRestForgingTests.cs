using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieEternalRestForgingTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-eternal-rest-forging");
    }

    [Fact]
    public async Task Play_installs_one_layer_of_power()
    {
        var card = await AddToHand<EternalRestForging>();

        await Play(card);

        var power = Player.Creature.GetPower<EternalRestForgingPower>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Equal([1], power.LayerGenerationCounts);
    }

    [Fact]
    public async Task Rest_discard_autoplay_installs_power()
    {
        var card = await AddToHand<EternalRestForging>();

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        var power = Player.Creature.GetPower<EternalRestForgingPower>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);

        await PlayCleanupAction();
    }

    [Fact]
    public async Task Owner_turn_start_generates_aurora_sword_to_hand()
    {
        await ClearCombatPiles();
        await Play(await AddToHand<EternalRestForging>());

        await TriggerPlayerTurnStartHooks();

        var sword = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Hand.GetPile(Player), sword.Pile);
    }

    [Fact]
    public async Task Multiple_layers_each_generate()
    {
        await ClearCombatPiles();

        await Play(await AddToHand<EternalRestForging>());
        await Play(await AddToHand<EternalRestForging>());

        await TriggerPlayerTurnStartHooks();

        Assert.Equal(2, PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>().Count());
        Assert.Equal(2, Player.Creature.GetPower<EternalRestForgingPower>()?.Amount);
    }

    [Fact]
    public async Task Upgraded_layer_generates_two_aurora_swords()
    {
        await ClearCombatPiles();
        var card = await AddToHand<EternalRestForging>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await Play(card);
        await TriggerPlayerTurnStartHooks();

        Assert.Equal(2, PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>().Count());
        var power = Player.Creature.GetPower<EternalRestForgingPower>();
        Assert.NotNull(power);
        Assert.Equal([2], power.LayerGenerationCounts);
    }

    [Fact]
    public async Task Generated_aurora_sword_overflows_to_discard_when_hand_is_full()
    {
        await ClearCombatPiles();
        await Play(await AddToHand<EternalRestForging>());

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<BattlemarkBond>();

        await TriggerPlayerTurnStartHooks();

        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Single(PileType.Discard.GetPile(Player).Cards.OfType<AuroraSword>());
    }

    private async Task TriggerPlayerTurnStartHooks()
    {
        await Hook.AfterPlayerTurnStart(Combat, new BlockingPlayerChoiceContext(), Player);
        await WaitForIdle();
    }

    private async Task PlayCleanupAction()
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(await AddToHand<BattlemarkBond>());
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
