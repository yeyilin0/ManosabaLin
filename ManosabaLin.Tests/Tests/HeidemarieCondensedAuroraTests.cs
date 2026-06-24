using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieCondensedAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-condensed-aurora");
    }

    [Fact]
    public async Task Upgraded_discard_returns_to_hand_before_transform_threshold()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CondensedAurora>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);
        Assert.Contains(card, PileType.Hand.GetPile(Player).Cards);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<LiberatedAurora>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_return_generates_aurora_swords_in_discard()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CondensedAurora>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Equal(
            card.DynamicVars[CondensedAurora.AuroraSwordsKey].IntValue,
            PileType.Discard.GetPile(Player).Cards.Count(c => c is IAuroraSwordCard));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Return_threshold_transforms_card_to_liberated_aurora()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CondensedAurora>();

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        var transformed = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<LiberatedAurora>());
        Assert.True(transformed.IsUpgraded == card.IsUpgraded);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<CondensedAurora>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_card_transforms_after_second_successful_return()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CondensedAurora>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();
        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        var transformed = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<LiberatedAurora>());
        Assert.True(transformed.IsUpgraded);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<CondensedAurora>());

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
