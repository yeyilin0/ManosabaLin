using MegaCrit.Sts2.Core.Rooms;
using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinRevokation()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int MaxHpLoss = 3;
    private const string AnanlinLoverEffectHoverLocEntry = "MANOSABA_LIN_CARD_ANANLIN_LOVER_EFFECT";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<AnanlinBrainwash>(),
        HoverTipFactory.FromCard<AnanlinLover>(),
        CardEffectHoverTipFactory.FromCard(
            ModelDb.Card<AnanlinLover>(),
            AnanlinLoverEffectHoverLocEntry),
        HoverTipFactory.FromKeyword(CardKeyword.Retain)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        await CreatureCmd.LoseMaxHp(choiceContext, Owner.Creature, MaxHpLoss, true);
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var removedCard = await ChooseAndConsumeBrainwash(choiceContext);
        var removedPower = Owner.Creature.GetPower<AnanlinBrainwashPower>() is not null;
        if (removedPower)
            await PowerCmd.Remove<AnanlinBrainwashPower>(Owner.Creature);

        RegisterBrainwashDeckCleanup();

        if (removedCard || removedPower)
            await AddRetainedLover();
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private async Task<bool> ChooseAndConsumeBrainwash(PlayerChoiceContext choiceContext)
    {
        var candidates = GetBrainwashCards().ToArray();
        if (candidates.Length == 0) return false;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1))).FirstOrDefault();
        if (selected is null) return false;

        if (selected.Pile is { IsCombatPile: true } pile)
        {
            if (pile.Type == PileType.Exhaust)
                await CardPileCmd.RemoveFromCombat(selected);
            else
                await CardCmd.Exhaust(choiceContext, selected);
            return true;
        }

        if (selected.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(selected, showPreview: false);
            return true;
        }

        return false;
    }

    private IEnumerable<CardModel> GetBrainwashCards()
    {
        return new[]
            {
                PileType.Hand,
                PileType.Draw,
                PileType.Discard,
                PileType.Exhaust,
                PileType.Deck
            }
            .SelectMany(pile => pile.GetPile(Owner).Cards)
            .Where(static card => card is AnanlinBrainwash);
    }

    private void RegisterBrainwashDeckCleanup()
    {
        var player = Owner;
        CombatManager.Instance.CombatEnded += OnCombatEnded;

        async void OnCombatEnded(CombatRoom room)
        {
            CombatManager.Instance.CombatEnded -= OnCombatEnded;

            var deckBrainwash = PileType.Deck.GetPile(player).Cards
                .Where(static card => card is AnanlinBrainwash)
                .ToArray();
            foreach (var card in deckBrainwash)
                await CardPileCmd.RemoveFromDeck(card, showPreview: false);
        }
    }

    private async Task AddRetainedLover()
    {
        var lover = CombatState.CreateCard<AnanlinLover>(Owner);
        lover.AddKeyword(CardKeyword.Retain);
        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.AddGeneratedCardToCombat(lover, PileType.Hand, Owner));
    }
}
