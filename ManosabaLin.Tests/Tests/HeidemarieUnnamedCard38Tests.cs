using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard38Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-038");
    }

    [Fact]
    public async Task Playing_auto_plays_top_draw_card_with_sts2_autoplay_result_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard38>();
        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var enemy = EnemyAt(0);
        var hpBefore = enemy.CurrentHp;

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        var autoPlay = Assert.Single(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, drawCard));

        Assert.True(autoPlay.CardPlay.IsAutoPlay);
        Assert.Equal(0, autoPlay.CardPlay.Resources.EnergySpent);
        Assert.NotNull(autoPlay.CardPlay.Target);
        Assert.True(enemy.CurrentHp < hpBefore);
        Assert.Same(PileType.Discard.GetPile(Player), drawCard.Pile);
        Assert.DoesNotContain(drawCard, PileType.Exhaust.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Empty_draw_and_discard_piles_are_safe_and_discard_portion_no_ops_with_empty_hand()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard38>();

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.DoesNotContain(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => entry.CardPlay.IsAutoPlay);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards);
        Assert.Same(PileType.Discard.GetPile(Player), card.Pile);
    }

    [Fact]
    public async Task Empty_draw_pile_uses_core_shuffle_before_autoplay()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard38>();
        var discardCard = await CreateCardInPile<HiroDefend>(PileType.Discard);

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, discardCard) && entry.CardPlay.IsAutoPlay);
        Assert.Same(PileType.Discard.GetPile(Player), discardCard.Pile);
    }

    [Fact]
    public async Task After_autoplay_one_selected_hand_card_is_discarded()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard38>();
        var chosenDiscard = await AddToHand<HiroAttack>();
        var keptCard = await AddToHand<HiroDefend>();
        var drawCard = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([chosenDiscard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, drawCard) && entry.CardPlay.IsAutoPlay);
        Assert.Same(PileType.Discard.GetPile(Player), chosenDiscard.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), keptCard.Pile);
    }

    [Fact]
    public async Task Discarded_hand_card_uses_normal_discard_path()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var card = await AddToHand<UnnamedCard38>();
        var discardedSword = await AddToHand<AuroraSword>();
        var hpBefore = enemy.CurrentHp;

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.True(enemy.CurrentHp < hpBefore);
        Assert.Same(PileType.Discard.GetPile(Player), discardedSword.Pile);
    }

    [Fact]
    public async Task Upgraded_play_discards_two_selected_hand_cards()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard38>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var firstDiscard = await AddToHand<HiroAttack>();
        var secondDiscard = await AddToHand<HiroDefend>();
        var keptCard = await AddToHand<HiroDefend>();
        var selector = new TestCardSelector();
        selector.PrepareToSelect([firstDiscard, secondDiscard]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), firstDiscard.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), secondDiscard.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), keptCard.Pile);
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
