using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieOldBladeBindingTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-old-blade-binding");
    }

    [Fact]
    public async Task No_attack_in_discard_is_noop()
    {
        await ClearCombatPiles();

        var card = await AddToHand<OldBladeBinding>();
        var nonAttack = await CreateCardInPile<HiroDefend>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), nonAttack.Pile);
        Assert.Empty(PileType.Hand.GetPile(Player).Cards.Where(c => c.Type == CardType.Attack));
    }

    [Fact]
    public async Task Upgraded_play_moves_all_available_when_fewer_than_count()
    {
        await ClearCombatPiles();

        var card = await AddToHand<OldBladeBinding>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var attack = await CreateCardInPile<HiroAttack>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), attack.Pile);
        Assert.Empty(PileType.Discard.GetPile(Player).Cards.Where(c => c.Type == CardType.Attack));
    }

    [Fact]
    public async Task Moved_attack_gains_link()
    {
        await ClearCombatPiles();

        var card = await AddToHand<OldBladeBinding>();
        var attack = await CreateCardInPile<HiroAttack>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), attack.Pile);
        Assert.True(HasComponent<LinkComponent>(attack));
    }

    [Fact]
    public async Task Token_attacks_are_candidates()
    {
        await ClearCombatPiles();

        var card = await AddToHand<OldBladeBinding>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var aurora = await CreateCardInPile<AuroraSword>(PileType.Discard);
        var crimson = await CreateCardInPile<CrimsonSword>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Hand.GetPile(Player), aurora.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), crimson.Pile);
    }

    [Fact]
    public async Task Upgrade_increases_move_count()
    {
        await ClearCombatPiles();

        var baseMoved = await PlayWithTwoAttacks(upgraded: false);
        await ClearCombatPiles();
        var upgradedMoved = await PlayWithTwoAttacks(upgraded: true);

        Assert.Equal(1, baseMoved);
        Assert.Equal(2, upgradedMoved);
    }

    [Fact]
    public async Task Link_cleanup_discards_old_snapshot_but_keeps_newly_moved_attack()
    {
        await ClearCombatPiles();

        var card = await AddToHand<OldBladeBinding>();
        var oldLinked = await AddToHand<HiroDefend>();
        oldLinked.TryAddComponent(new LinkComponent());
        var attack = await CreateCardInPile<HiroAttack>(PileType.Discard);

        await PlayWithEnergy(card);

        Assert.Same(PileType.Discard.GetPile(Player), oldLinked.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), attack.Pile);
        Assert.True(HasComponent<LinkComponent>(attack));
    }

    private async Task<int> PlayWithTwoAttacks(bool upgraded)
    {
        var card = await AddToHand<OldBladeBinding>();
        if (upgraded)
            CardCmd.Upgrade(card, CardPreviewStyle.None);

        await CreateCardInPile<HiroAttack>(PileType.Discard);
        await CreateCardInPile<HiroAttack>(PileType.Discard);

        await PlayWithEnergy(card);

        return PileType.Hand.GetPile(Player).Cards.Count(c => c.Type == CardType.Attack);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private static bool HasComponent<T>(CardModel card)
        where T : class, ICardComponent
    {
        return ((IComponentsCardModel)card).GetComponent<T>() != null;
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
