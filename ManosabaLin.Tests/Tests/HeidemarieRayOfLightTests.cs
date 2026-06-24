using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieRayOfLightTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-ray-of-light");
    }

    [Fact]
    public async Task Playing_gains_energy()
    {
        await ClearCombatPiles();

        var card = await AddToHand<RayOfLight>();

        await PlayWithExactCost(card);

        Assert.Equal(card.DynamicVars.Energy.IntValue, Player.PlayerCombatState!.Energy);
    }

    [Fact]
    public async Task Rest_discard_autoplay_gains_energy()
    {
        await ClearCombatPiles();

        var card = await AddToHand<RayOfLight>();

        await PlayerCmd.SetEnergy(0, Player);
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Equal(card.DynamicVars.Energy.IntValue, Player.PlayerCombatState!.Energy);
        AssertRestAutoPlayed(card);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_non_link_discard_gains_only_base_energy_even_when_card_has_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<RayOfLight>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        card.TryAddComponent(new LinkComponent());

        await PlayerCmd.SetEnergy(0, Player);
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Equal(card.DynamicVars.Energy.IntValue, Player.PlayerCombatState!.Energy);
        AssertRestAutoPlayed(card);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Base_link_cleanup_discard_gains_only_base_energy()
    {
        await ClearCombatPiles();

        var played = await AddToHand<GlimmerHarvest>();
        var card = await AddToHand<RayOfLight>();
        card.TryAddComponent(new LinkComponent());

        await PlayWithExactCost(played);

        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
        Assert.Equal(card.DynamicVars.Energy.IntValue, Player.PlayerCombatState!.Energy);
        AssertRestAutoPlayed(card);
    }

    [Fact]
    public async Task Upgraded_link_cleanup_discard_with_link_gains_bonus_energy()
    {
        await ClearCombatPiles();

        var played = await AddToHand<GlimmerHarvest>();
        var card = await AddToHand<RayOfLight>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        card.TryAddComponent(new LinkComponent());
        var expectedEnergy = card.DynamicVars.Energy.IntValue * 2;

        await PlayWithExactCost(played);

        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
        Assert.Equal(expectedEnergy, Player.PlayerCombatState!.Energy);
        AssertRestAutoPlayed(card);
    }

    private async Task PlayWithExactCost(CardModel card)
    {
        await PlayerCmd.SetEnergy(card.EnergyCost.GetAmountToSpend(), Player);
        await WaitForIdle();
        await Play(card);
    }

    private static void AssertRestAutoPlayed(CardModel card)
    {
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);
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
