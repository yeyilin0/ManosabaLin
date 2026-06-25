using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieCrimsonEdgeSleepPactTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-crimson-edge-sleep-pact");
    }

    [Fact]
    public async Task Play_discards_selected_hand_card_with_bounce_without_generating_crimson_sword()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeSleepPact>();
        var discarded = await AddToHand<HiroAttack>();
        var kept = await AddToHand<HiroDefend>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), kept.Pile);
        AssertBounce(discarded, 1m);
        Assert.Equal(0, CountCombatCards<CrimsonSword>());
    }

    [Fact]
    public async Task Upgraded_play_increases_attached_bounce_but_not_discard_or_generation_counts()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeSleepPact>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var discarded = await AddToHand<HiroAttack>();
        var kept = await AddToHand<HiroDefend>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), kept.Pile);
        AssertBounce(discarded, 2m);
        Assert.Equal(0, CountCombatCards<CrimsonSword>());
    }

    [Fact]
    public async Task AutoPlay_only_runs_normal_discard_and_bounce_effect_without_crimson_generation()
    {
        await ClearCombatPiles();

        var card = Combat.CreateCard<CrimsonEdgeSleepPact>(Player);
        var discarded = await AddToHand<HiroAttack>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);
        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        AssertBounce(discarded, 1m);
        Assert.Equal(0, CountCombatCards<CrimsonSword>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Rest_discard_generates_crimson_sword_then_autoplays_normal_discard_and_bounce_effect()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeSleepPact>();
        var discarded = await AddToHand<HiroAttack>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), card);
        await WaitForIdle();

        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, card) && entry.CardPlay.IsAutoPlay);
        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<CrimsonSword>());
        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        AssertBounce(discarded, 1m);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Discarded_bounced_card_returns_to_hand_on_turn_start()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeSleepPact>();
        var discarded = await AddToHand<HiroAttack>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([discarded]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), discarded.Pile);
        AssertBounce(discarded, 1m);

        await Hook.BeforeSideTurnStart(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), discarded.Pile);
        Assert.Null(GetBounce(discarded));
    }

    [Fact]
    public async Task Empty_hand_play_is_safe_and_does_not_generate_crimson_sword()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeSleepPact>();

        await PlayWithEnergy(card);

        Assert.Empty(PileType.Hand.GetPile(Player).Cards);
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
        Assert.Equal(0, CountCombatCards<CrimsonSword>());
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
        await WaitForIdle();
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
        await WaitForIdle();
    }

    private int CountCombatCards<TCard>()
        where TCard : CardModel
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
            .Sum(pileType => pileType.GetPile(Player).Cards.OfType<TCard>().Count());
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
