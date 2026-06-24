using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieAuroraCrimsonTwinbirthTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-aurora-crimson-twinbirth");
    }

    [Fact]
    public async Task Play_installs_aurora_crimson_twinbirth_power()
    {
        await ClearCombatPiles();

        var card = await AddToHand<AuroraCrimsonTwinbirth>();
        await PlayWithEnergy(card);

        var power = Player.Creature.GetPower<AuroraCrimsonTwinbirthPower>();
        Assert.NotNull(power);
        Assert.Equal(card.DynamicVars[AuroraCrimsonTwinbirth.CrimsonSwordsKey].BaseValue, power.Amount);
    }

    [Fact]
    public async Task Aurora_generation_adds_crimson_swords_to_hand()
    {
        await ClearCombatPiles();
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 1, Player.Creature);

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(1, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Crimson_generation_does_not_trigger_more_crimson_generation()
    {
        await ClearCombatPiles();
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 3, Player.Creature);

        var result = await SwordTokenGeneration.AddCrimsonSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(1, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Original_aurora_request_triggers_even_when_replaced_with_crimson()
    {
        await ClearCombatPiles();
        await ApplyPower<AuroraToCrimsonReplacementPower>(Player.Creature, 1, Player.Creature);
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 1, Player.Creature);

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(SwordTokenKind.Aurora, result.Request.OriginalKind);
        Assert.Equal(0, result.SuccessCountFor(SwordTokenKind.Aurora));
        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Crimson));
        Assert.Equal(0, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(2, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Upgraded_card_only_lowers_cost_without_increasing_crimson_generation()
    {
        await ClearCombatPiles();

        var card = await AddToHand<AuroraCrimsonTwinbirth>();
        var baseAmount = card.DynamicVars[AuroraCrimsonTwinbirth.CrimsonSwordsKey].BaseValue;
        CardCmd.Upgrade(card, CardPreviewStyle.None);

        Assert.Equal(0, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(baseAmount, card.DynamicVars[AuroraCrimsonTwinbirth.CrimsonSwordsKey].BaseValue);

        await PlayWithEnergy(card);
        var power = Player.Creature.GetPower<AuroraCrimsonTwinbirthPower>();
        Assert.NotNull(power);
        Assert.Equal(baseAmount, power.Amount);

        await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(baseAmount, (decimal)CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Power_stacks_add_one_crimson_sword_each()
    {
        await ClearCombatPiles();
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 2, Player.Creature);

        await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, CountCards<AuroraSword>(PileType.Hand));
        Assert.Equal(2, CountCards<CrimsonSword>(PileType.Hand));

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Full_hand_success_still_triggers_extra_crimson_generation()
    {
        await ClearCombatPiles();
        await ApplyPower<AuroraCrimsonTwinbirthPower>(Player.Creature, 1, Player.Creature);

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<LinkedEdge>();

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new BlockingPlayerChoiceContext(),
            Player,
            1);
        await WaitForIdle();

        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(0, result.EnteredHandCountFor(SwordTokenKind.Aurora));
        Assert.Equal(CardPile.MaxCardsInHand, hand.Cards.Count);
        Assert.Equal(1, CountCards<AuroraSword>(PileType.Discard));
        Assert.Equal(1, CountCards<CrimsonSword>(PileType.Discard));

        await ExecuteRunnerAction();
    }

    private async Task PlayWithEnergy(CardModel card)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card);
    }

    private async Task PlayWithEnergy(CardModel card, Creature target)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    private async Task ExecuteRunnerAction()
    {
        var hand = PileType.Hand.GetPile(Player);
        var actionCard = hand.Cards.OfType<LinkedEdge>().FirstOrDefault()
            ?? await AddToHand<LinkedEdge>();

        await PlayWithEnergy(actionCard, EnemyAt(0));
    }

    private int CountCards<TCard>(PileType pileType)
        where TCard : CardModel
    {
        return pileType.GetPile(Player).Cards.OfType<TCard>().Count();
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

[RegisterPower]
public sealed class AuroraToCrimsonReplacementPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        if (request.OriginalKind == SwordTokenKind.Aurora)
            request.ReplaceWith(SwordTokenKind.Crimson, request.OriginalCount);

        return Task.CompletedTask;
    }
}
