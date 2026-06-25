using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie;
using ManosabaLin.Characters.Heidemarie.Cards;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieCrimsonEdgeReturnPactTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Heidemarie>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-crimson-edge-return-pact");
    }

    [Fact]
    public async Task Play_generates_crimson_sword_with_bounce()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeReturnPact>();
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        var generated = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<CrimsonSword>());
        Assert.Equal(1m, GetBounce(generated)?.Amount);
    }

    [Fact]
    public async Task Upgrade_generates_two_crimson_swords()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeReturnPact>();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        var generated = PileType.Hand.GetPile(Player).Cards.OfType<CrimsonSword>().ToArray();
        Assert.Equal(2, generated.Length);
        Assert.All(generated, sword => Assert.Equal(1m, GetBounce(sword)?.Amount));
    }

    [Fact]
    public async Task Generated_crimson_sword_bounces_back_from_draw_pile()
    {
        await ClearCombatPiles();

        var card = await AddToHand<CrimsonEdgeReturnPact>();
        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        var generated = Assert.Single(PileType.Hand.GetPile(Player).Cards.OfType<CrimsonSword>());
        await CardPileCmd.Add(generated, PileType.Draw);
        await WaitForIdle();

        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Hand.GetPile(Player), generated.Pile);
        Assert.Null(GetBounce(generated));
    }

    [Fact]
    public async Task Full_hand_overflow_to_discard_still_attaches_bounce()
    {
        await ClearCombatPiles();

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();

        var card = Combat.CreateCard<CrimsonEdgeReturnPact>(Player);
        await CardCmd.AutoPlay(
            new BlockingPlayerChoiceContext(),
            card,
            null,
            skipCardPileVisuals: true);
        await WaitForIdle();

        var generated = Assert.Single(PileType.Discard.GetPile(Player).Cards.OfType<CrimsonSword>());
        Assert.Equal(1m, GetBounce(generated)?.Amount);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Sword_generation_listeners_observe_generated_crimson_sword()
    {
        await ClearCombatPiles();

        var listener = await ApplyPower<CrimsonEdgeReturnPactGenerationListenerPower>(
            Player.Creature,
            1,
            Player.Creature);
        Assert.NotNull(listener);
        var card = await AddToHand<CrimsonEdgeReturnPact>();

        await PlayerCmd.SetEnergy(10, Player);
        await Play(card);

        Assert.Equal([SwordTokenKind.Crimson], listener.RequestedOriginalKinds);
        Assert.Equal([SwordTokenKind.Crimson], listener.SucceededOriginalKinds);
        Assert.Equal([1], listener.SucceededSuccessCounts);
    }

    private async Task TriggerPlayerTurnStartHooks()
    {
        await Hook.BeforeSideTurnStart(Combat, CombatSide.Player, [Player.Creature]);
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

    private async Task ExecuteRunnerAction()
    {
        var hand = PileType.Hand.GetPile(Player);
        if (hand.Cards.Count >= CardPile.MaxCardsInHand)
        {
            await CardPileCmd.RemoveFromCombat(hand.Cards[0], skipVisuals: true);
            await WaitForIdle();
        }

        var actionCard = await AddToHand<HiroAttack>();
        await PlayerCmd.SetEnergy(10, Player);
        await WaitForIdle();
        await Play(actionCard, EnemyAt(0));
    }

    private static BounceComponent? GetBounce(CardModel card)
    {
        return Assert.IsAssignableFrom<IComponentsCardModel>(card).GetComponent<BounceComponent>();
    }
}

[RegisterPower]
public sealed class CrimsonEdgeReturnPactGenerationListenerPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public List<SwordTokenKind> RequestedOriginalKinds { get; } = [];
    public List<SwordTokenKind> SucceededOriginalKinds { get; } = [];
    public List<int> SucceededSuccessCounts { get; } = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        RequestedOriginalKinds.Add(request.OriginalKind);
        return Task.CompletedTask;
    }

    public Task OnSwordGenerationSucceeded(
        PlayerChoiceContext choiceContext,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        SucceededOriginalKinds.Add(request.OriginalKind);
        SucceededSuccessCounts.Add(result.SuccessCountFor(SwordTokenKind.Crimson));
        return Task.CompletedTask;
    }
}
