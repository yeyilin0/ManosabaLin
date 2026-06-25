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
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieUnfoldAuroraTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-unfold-aurora");
    }

    [Fact]
    public async Task Card_has_link_component()
    {
        var card = await AddToHand<UnfoldAurora>();

        Assert.True(((CardModel)card).HasComponent<LinkComponent>());
        await PlayWithEnergy(card);
    }

    [Fact]
    public async Task Playing_counts_itself_as_linked_card()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnfoldAurora>();

        await PlayWithEnergy(card);

        var generated = PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>().ToArray();
        Assert.Single(generated);
        Assert.Same(PileType.Hand.GetPile(Player), generated[0].Pile);
    }

    [Fact]
    public async Task Playing_generates_before_link_cleanup_and_discards_old_linked_cards()
    {
        await ClearCombatPiles();

        var card = await AddToHand<UnfoldAurora>();
        var linkedAttack = await AddToHand<HiroAttack>();
        var linkedSkill = await AddToHand<HiroDefend>();
        var unlinkedSkill = await AddToHand<HiroDefend>();
        linkedAttack.TryAddComponent(new LinkComponent());
        linkedSkill.TryAddComponent(new LinkComponent());

        await PlayWithEnergy(card);

        var generated = PileType.Hand.GetPile(Player).Cards.OfType<AuroraSword>().ToArray();
        Assert.Equal(3, generated.Length);
        Assert.All(generated, sword => Assert.Same(PileType.Hand.GetPile(Player), sword.Pile));
        Assert.Same(PileType.Discard.GetPile(Player), linkedAttack.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), linkedSkill.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unlinkedSkill.Pile);
    }

    [Fact]
    public async Task Upgrade_only_lowers_cost()
    {
        var card = await AddToHand<UnfoldAurora>();
        var baseLinkedCardsPerSword = card.DynamicVars[UnfoldAurora.LinkedCardsPerAuroraSwordKey].BaseValue;
        var baseSwordsPerBatch = card.DynamicVars[UnfoldAurora.AuroraSwordsKey].BaseValue;

        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(baseLinkedCardsPerSword, card.DynamicVars[UnfoldAurora.LinkedCardsPerAuroraSwordKey].BaseValue);
        Assert.Equal(baseSwordsPerBatch, card.DynamicVars[UnfoldAurora.AuroraSwordsKey].BaseValue);
        await PlayWithEnergy(card);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
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
