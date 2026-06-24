using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MinionLib.Component.Interfaces;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieMechanicComponentTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Hiro>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-components");
    }

    [Fact]
    public async Task Rest_auto_plays_after_explicit_hand_discard_only()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var movedOnly = await AddToHand<HiroAttack>();
        movedOnly.TryAddComponent(new RestComponent());

        var hpBeforeMove = enemy.CurrentHp;
        await CardPileCmd.Add(movedOnly, PileType.Discard);
        await WaitForIdle();

        Assert.Equal(hpBeforeMove, enemy.CurrentHp);
        Assert.DoesNotContain(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, movedOnly));

        var discarded = await AddToHand<HiroAttack>();
        discarded.TryAddComponent(new RestComponent());

        var hpBeforeDiscard = enemy.CurrentHp;
        await CardCmd.Discard(new BlockingPlayerChoiceContext(), discarded);
        await WaitForIdle();

        Assert.True(enemy.CurrentHp < hpBeforeDiscard);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, discarded) && entry.CardPlay.IsAutoPlay);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Link_manual_play_discards_snapshot_and_rest_discard_still_triggers()
    {
        await ClearCombatPiles();

        var enemy = EnemyAt(0);
        var played = await AddToHand<HiroAttack>();
        var linkedSkill = await AddToHand<HiroDefend>();
        var linkedRestAttack = await AddToHand<HiroAttack>();
        var unlinked = await AddToHand<HiroDefend>();

        played.TryAddComponent(new LinkComponent());
        linkedSkill.TryAddComponent(new LinkComponent());
        linkedRestAttack.TryAddComponent(new LinkComponent());
        linkedRestAttack.TryAddComponent(new RestComponent());

        await PlayerCmd.SetEnergy(10, Player);
        await Play(played, enemy);

        Assert.Same(PileType.Discard.GetPile(Player), linkedSkill.Pile);
        Assert.Same(PileType.Discard.GetPile(Player), linkedRestAttack.Pile);
        Assert.Same(PileType.Hand.GetPile(Player), unlinked.Pile);
        Assert.Contains(
            CombatManager.Instance.History.CardPlaysFinished,
            entry => ReferenceEquals(entry.CardPlay.Card, linkedRestAttack) && entry.CardPlay.IsAutoPlay);
    }

    [Fact]
    public async Task Link_autoplay_does_not_cleanup_other_linked_cards()
    {
        await ClearCombatPiles();

        var played = await AddToHand<HiroAttack>();
        var otherLinked = await AddToHand<HiroDefend>();
        played.TryAddComponent(new LinkComponent());
        otherLinked.TryAddComponent(new LinkComponent());

        await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), played, EnemyAt(0), skipCardPileVisuals: true);
        await WaitForIdle();

        Assert.Same(PileType.Hand.GetPile(Player), otherLinked.Pile);

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Bounce_returns_only_when_hand_has_room_and_consumes_on_success()
    {
        await ClearCombatPiles();

        var bouncingCard = await CreateCardInPile<HiroDefend>(PileType.Discard);
        bouncingCard.TryAddComponent(new BounceComponent(1));

        var fillers = new List<CardModel>();
        for (var i = 0; i < CardPile.MaxCardsInHand; i++)
            fillers.Add(await CreateCardInPile<HiroDefend>(PileType.Hand));

        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Discard.GetPile(Player), bouncingCard.Pile);
        Assert.Equal(1, ((IComponentsCardModel)bouncingCard).GetComponent<BounceComponent>()?.Amount);

        await CardPileCmd.RemoveFromCombat(fillers[0], skipVisuals: true);
        await WaitForIdle();
        await TriggerPlayerTurnStartHooks();

        Assert.Same(PileType.Hand.GetPile(Player), bouncingCard.Pile);
        Assert.Null(((IComponentsCardModel)bouncingCard).GetComponent<BounceComponent>());

        await ExecuteRunnerAction();
    }

    [Fact]
    public async Task Sword_grave_stays_in_discard_during_discard_to_draw_shuffle()
    {
        await ClearCombatPiles();

        var swordGrave = await CreateCardInPile<HiroDefend>(PileType.Discard);
        var normalCard = await CreateCardInPile<HiroAttack>(PileType.Discard);
        swordGrave.TryAddComponent(new SwordGraveComponent());

        await CardPileCmd.Shuffle(new BlockingPlayerChoiceContext(), Player);
        await WaitForIdle();

        Assert.Same(PileType.Discard.GetPile(Player), swordGrave.Pile);
        Assert.Same(PileType.Draw.GetPile(Player), normalCard.Pile);

        await ExecuteRunnerAction();
    }

    private async Task TriggerPlayerTurnStartHooks()
    {
        await Hook.BeforeSideTurnStart(Combat, CombatSide.Player, [Player.Creature]);
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
}
