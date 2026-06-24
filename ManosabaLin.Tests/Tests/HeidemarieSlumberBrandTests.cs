using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieSlumberBrandTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-slumber-brand");
    }

    [Fact]
    public async Task Card_shape_matches_design()
    {
        await ClearCombatPiles();

        var card = await AddToHand<SlumberBrand>();

        Assert.Equal(1, card.EnergyCost.GetWithModifiers(CostModifiers.None));
        Assert.Equal(CardType.Skill, card.Type);
        Assert.Equal(CardRarity.Uncommon, card.Rarity);
        Assert.Equal(TargetType.Self, card.TargetType);
        Assert.True(HasComponent<RestComponent>(card));

        await PlayWithEnergy(card);
    }

    [Fact]
    public async Task Next_attack_gains_rest_before_it_resolves()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        var attack = await AddToHand<SlumberBrandProbeAttack>();

        await PlayWithEnergy(brand);
        await AutoPlayAttack(attack);

        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
        Assert.False(Player.Creature.HasPower<SlumberBrandPower>());
    }

    [Fact]
    public async Task Non_attack_does_not_consume_listener()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        var skill = await AddToHand<HiroDefend>();
        var attack = await AddToHand<SlumberBrandProbeAttack>();

        await PlayWithEnergy(brand);
        await PlayWithEnergy(skill);

        Assert.False(HasComponent<RestComponent>(skill));
        Assert.True(Player.Creature.HasPower<SlumberBrandPower>());

        await PlayWithEnergy(attack, EnemyAt(0));

        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
        Assert.False(Player.Creature.HasPower<SlumberBrandPower>());
    }

    [Fact]
    public async Task Listener_triggers_once()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        var firstAttack = await AddToHand<SlumberBrandProbeAttack>();
        var secondAttack = await AddToHand<SlumberBrandProbeAttack>();

        await PlayWithEnergy(brand);
        await PlayWithEnergy(firstAttack, EnemyAt(0));
        await PlayWithEnergy(secondAttack, EnemyAt(0));

        Assert.True(firstAttack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(firstAttack));
        Assert.False(secondAttack.HadRestDuringOnPlay);
        Assert.False(HasComponent<RestComponent>(secondAttack));
    }

    [Fact]
    public async Task Untriggered_listener_is_removed_at_turn_end()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        var attack = await AddToHand<SlumberBrandProbeAttack>();

        await PlayWithEnergy(brand);
        await TriggerPlayerTurnEndHooks();
        await PlayWithEnergy(attack, EnemyAt(0));

        Assert.False(attack.HadRestDuringOnPlay);
        Assert.False(HasComponent<RestComponent>(attack));
        Assert.False(Player.Creature.HasPower<SlumberBrandPower>());
    }

    [Fact]
    public async Task Upgraded_listener_returns_attack_to_hand_after_play()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        CardCmd.Upgrade(brand, CardPreviewStyle.None);
        var attack = await AddToHand<SlumberBrandProbeAttack>();

        await PlayWithEnergy(brand);
        await AutoPlayAttack(attack);

        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
        Assert.Same(PileType.Hand.GetPile(Player), attack.Pile);
        Assert.False(Player.Creature.HasPower<SlumberBrandPower>());
    }

    [Fact]
    public async Task Upgraded_listener_returns_exhaust_attack_to_hand()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        CardCmd.Upgrade(brand, CardPreviewStyle.None);
        var attack = await AddToHand<SlumberBrandExhaustProbeAttack>();

        await PlayWithEnergy(brand);
        await AutoPlayAttack(attack);

        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
        Assert.Same(PileType.Hand.GetPile(Player), attack.Pile);
        Assert.DoesNotContain(attack, PileType.Exhaust.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Upgraded_listener_does_not_return_attack_directly_removed_from_combat()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        CardCmd.Upgrade(brand, CardPreviewStyle.None);
        var attack = await AddToHand<SlumberBrandRemoveFromCombatProbeAttack>();

        await PlayWithEnergy(brand);
        await PlayWithEnergy(attack, EnemyAt(0));

        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
        Assert.Null(attack.Pile);
        Assert.DoesNotContain(attack, PileType.Hand.GetPile(Player).Cards);
    }

    [Fact]
    public async Task Rest_discard_autoplay_installs_same_listener()
    {
        await ClearCombatPiles();

        var brand = await AddToHand<SlumberBrand>();
        var attack = await AddToHand<SlumberBrandProbeAttack>();

        await CardCmd.Discard(new BlockingPlayerChoiceContext(), brand);
        await WaitForIdle();
        await PlayWithEnergy(attack, EnemyAt(0));

        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, brand) && entry.CardPlay.IsAutoPlay);
        Assert.True(attack.HadRestDuringOnPlay);
        Assert.True(HasComponent<RestComponent>(attack));
    }

    private async Task PlayWithEnergy(CardModel card, Creature? target = null)
    {
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(card, target);
    }

    private async Task AutoPlayAttack(CardModel card)
    {
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            EnemyAt(0),
            skipCardPileVisuals: true);
        await WaitForIdle();
    }

    private async Task TriggerPlayerTurnEndHooks()
    {
        await Hook.AfterTurnEnd(Combat, CombatSide.Player, [Player.Creature]);
        await WaitForIdle();
    }

    private static bool HasComponent<T>(CardModel card)
        where T : class, ICardComponent
    {
        return ((IComponentsCardModel)card).GetComponent<T>() != null;
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

[RegisterCard(typeof(LinCardPool))]
public sealed class SlumberBrandProbeAttack()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public bool HadRestDuringOnPlay { get; private set; }

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        HadRestDuringOnPlay = ((CardModel)this).HasComponent<RestComponent>();
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(LinCardPool))]
public sealed class SlumberBrandExhaustProbeAttack()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public bool HadRestDuringOnPlay { get; private set; }

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        HadRestDuringOnPlay = ((CardModel)this).HasComponent<RestComponent>();
        return Task.CompletedTask;
    }
}

[RegisterCard(typeof(LinCardPool))]
public sealed class SlumberBrandRemoveFromCombatProbeAttack()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public bool HadRestDuringOnPlay { get; private set; }

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        HadRestDuringOnPlay = ((CardModel)this).HasComponent<RestComponent>();
        await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
    }
}
