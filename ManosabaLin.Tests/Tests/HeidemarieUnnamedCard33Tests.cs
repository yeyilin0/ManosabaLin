using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.TestSupport;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnnamedCard33Tests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-card-033");
    }

    [Fact]
    public async Task Playing_base_generates_one_aurora_sword_and_draws_one_card()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard33>();
        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Hand.GetPile(Player), drawCard.Pile);
    }

    [Fact]
    public async Task Playing_generates_before_drawing_when_only_one_hand_slot_is_open()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard33>();
        while (PileType.Hand.GetPile(Player).Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();

        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        var generated = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Hand.GetPile(Player), generated.Pile);
        Assert.Same(PileType.Draw.GetPile(Player), drawCard.Pile);
    }

    [Fact]
    public async Task Upgraded_play_uses_base_generation_count_and_upgraded_draw_and_discard_counts()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard33>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var firstDrawn = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var secondDrawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([firstDrawn, secondDrawn]);

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Discard.GetPile(Player), firstDrawn.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), secondDrawn.Pile);
    }

    [Fact]
    public async Task Upgraded_discard_is_optional()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard33>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var firstDrawn = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var secondDrawn = await CreateCardInPile<HiroDefend>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect(Array.Empty<CardModel>());

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Hand.GetPile(Player), firstDrawn.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), secondDrawn.Pile);
    }

    [Fact]
    public async Task Upgraded_discard_uses_normal_discard_path()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var card = await AddToHand<UnnamedCard33>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var selector = new TestCardSelector();
        selector.PrepareToSelect([0]);
        var hpBefore = enemy.CurrentHp;

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.True(enemy.CurrentHp < hpBefore);
        Assert.Single(PileType.Discard.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
    }

    [Fact]
    public async Task Upgraded_generation_uses_base_count_when_hand_has_one_open_slot()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnnamedCard33>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        while (PileType.Hand.GetPile(Player).Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();

        var drawCard = await CreateCardInPile<HiroAttack>(PileType.Draw);
        var selector = new TestCardSelector();
        selector.PrepareToSelect(Array.Empty<CardModel>());

        using var selection = CardSelectCmd.UseSelector(selector);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);
        await WaitForIdle();

        Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Empty(PileType.Discard.GetPile(Player).Cards.OfType<AuroraSword>());
        Assert.Same(PileType.Draw.GetPile(Player), drawCard.Pile);
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
