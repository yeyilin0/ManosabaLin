using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieCitadelAuroraReleaseTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-citadel-aurora-release");
    }

    [Fact]
    public async Task Play_installs_single_configured_power()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CitadelAuroraRelease>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<CitadelAuroraReleasePower>();
        Assert.NotNull(power);
        Assert.Equal(1, power.Amount);
        Assert.Equal(card.DynamicVars[CitadelAuroraRelease.SwordThresholdKey].IntValue, power.SwordThreshold);
        Assert.Equal(card.DynamicVars[CitadelAuroraRelease.AuroraChainGainKey].IntValue, power.AuroraChainGain);
    }

    [Fact]
    public async Task Discard_swords_at_threshold_gain_aurora_chain_at_turn_end()
    {
        await ClearCombatPiles();

        await PlayWithEnergy(await AddToHand<CitadelAuroraRelease>());
        await CreateCardInPile<AuroraSword>(PileType.Discard);
        await CreateCardInPile<CrimsonSword>(PileType.Discard);

        await TriggerPlayerTurnEndHooks();

        var auroraChain = Player.Creature.GetPower<AuroraChainPower>();
        Assert.NotNull(auroraChain);
        Assert.Equal(1, auroraChain.Amount);
    }

    [Fact]
    public async Task Non_aurora_or_crimson_sword_grave_cards_do_not_count()
    {
        await ClearCombatPiles();

        await PlayWithEnergy(await AddToHand<CitadelAuroraRelease>());
        await CreateCardInPile<AuroraSword>(PileType.Discard);
        var nonSword = await CreateCardInPile<HiroDefend>(PileType.Discard);
        nonSword.TryAddComponent(new SwordGraveComponent());

        await TriggerPlayerTurnEndHooks();

        Assert.Null(Player.Creature.GetPower<AuroraChainPower>());
    }

    [Fact]
    public async Task Upgraded_card_lowers_threshold_only()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CitadelAuroraRelease>();
        var baseGain = card.DynamicVars[CitadelAuroraRelease.AuroraChainGainKey].IntValue;
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(1, card.DynamicVars[CitadelAuroraRelease.SwordThresholdKey].IntValue);
        Assert.Equal(baseGain, card.DynamicVars[CitadelAuroraRelease.AuroraChainGainKey].IntValue);

        await PlayWithEnergy(card);
        await CreateCardInPile<AuroraSword>(PileType.Discard);
        await TriggerPlayerTurnEndHooks();

        var auroraChain = Player.Creature.GetPower<AuroraChainPower>();
        Assert.NotNull(auroraChain);
        Assert.Equal(baseGain, auroraChain.Amount);
    }

    [Fact]
    public async Task Multiple_plays_do_not_stack_effect()
    {
        await ClearCombatPiles();

        await PlayWithEnergy(await AddToHand<CitadelAuroraRelease>());
        await PlayWithEnergy(await AddToHand<CitadelAuroraRelease>());
        await CreateCardInPile<AuroraSword>(PileType.Discard);
        await CreateCardInPile<CrimsonSword>(PileType.Discard);

        await TriggerPlayerTurnEndHooks();

        var release = Player.Creature.GetPower<CitadelAuroraReleasePower>();
        Assert.NotNull(release);
        Assert.Equal(1, release.Amount);
        Assert.Equal(1, Player.Creature.GetPower<AuroraChainPower>()?.Amount);
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task TriggerPlayerTurnEndHooks()
    {
        await Hook.AfterTurnEnd(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();
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
