using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieSwordRainTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-sword-rain");
    }

    [Fact]
    public async Task Drawn_card_generates_aurora_swords()
    {
        await ClearCombatPiles();

        var card = await CreateCardInPile<SwordRain>(PileType.Draw);

        var drawn = (await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player)).ToArray();
        await WaitForIdle();

        Assert.Contains(card, drawn);
        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);
        Assert.Equal(card.DynamicVars[SwordRain.AuroraSwordsKey].IntValue, CountCards<AuroraSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Non_draw_hand_entry_does_not_generate_aurora_swords()
    {
        await ClearCombatPiles();

        var card = await CreateCardInPile<SwordRain>(PileType.Discard);

        await CardPileCmd.Add(card, PileType.Hand);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);
        Assert.Equal(0, CountAuroraSwordsInCombat());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Base_play_gains_block_without_generating_aurora_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<SwordRain>();
        var blockBefore = Player.Creature.Block;

        await PlayWithEnergy(card);

        Assert.Equal(blockBefore + card.DynamicVars.Block.BaseValue, Player.Creature.Block);
        Assert.Equal(0, CountAuroraSwordsInCombat());
    }

    [Fact]
    public async Task Upgraded_play_gains_block_and_generates_aurora_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<SwordRain>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        var blockBefore = Player.Creature.Block;
        var expectedSwords = card.DynamicVars[SwordRain.AuroraSwordsKey].IntValue;

        await PlayWithEnergy(card);

        Assert.Equal(blockBefore + card.DynamicVars.Block.BaseValue, Player.Creature.Block);
        Assert.Equal(expectedSwords, CountCards<AuroraSword>(PileType.Hand));
    }

    [Fact]
    public async Task Full_hand_draw_generation_overflows_to_discard()
    {
        await ClearCombatPiles();

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand - 1)
            await AddToHand<HiroDefend>();

        var card = await CreateCardInPile<SwordRain>(PileType.Draw);
        var expectedSwords = card.DynamicVars[SwordRain.AuroraSwordsKey].IntValue;

        await CardPileCmd.Draw(new BlockingPlayerChoiceContext(), 1, Player);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), card.Pile);
        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Equal(0, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(expectedSwords, CountCards<AuroraSword>(PileType.Discard));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Sword_rain_is_registered_as_heidemarie_starter_card()
    {
        await WaitForIdle();

        Assert.Contains(Player.Deck.Cards, card => card is SwordRain);

        await ExecuteRunnerAction();
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task ExecuteRunnerAction()
    {
        var actionCard = PileType.Hand.GetPile(Player).Cards.OfType<HiroDefend>().FirstOrDefault()
            ?? await AddToHand<HiroDefend>();

        await PlayWithEnergy(actionCard);
        await WaitForIdle();
    }

    private int CountAuroraSwordsInCombat()
    {
        return CountCards<AuroraSword>(PileType.Hand)
            + CountCards<AuroraSword>(PileType.Draw)
            + CountCards<AuroraSword>(PileType.Discard)
            + CountCards<AuroraSword>(PileType.Exhaust);
    }

    private int CountCards<TCard>(PileType pileType)
        where TCard : CardModel
    {
        return pileType.GetPile(Player).Cards.OfType<TCard>().Count();
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
