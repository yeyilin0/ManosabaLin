using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Anandeath() : ManosabaCardTemplate(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies)
{
    private const int OtherCardsPerGift = 10;
    private const int WithLoss = 50;
    private const int SuspectLoss = 3;

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("OtherCardsPerGift", OtherCardsPerGift),
        new PowerVar<WithPower>("WithLoss", WithLoss),
        new PowerVar<SuspectPower>("SuspectLoss", SuspectLoss)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        if (CombatState is null) return;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);
        AnanlinSilenceIntentManager.ApplyRandomReplacementIntentToAllEnemies(owner);

        var giftCount = CountOtherCardPoolCardsPlayedThisCombat() / OtherCardsPerGift;
        var giftCandidates = GetRecordedGiftCandidates().ToArray();
        var rng = owner.RunState.Rng.CombatCardGeneration;

        var teammates = CombatState.GetTeammatesOf(creature)
            .Where(static teammate => teammate is { IsAlive: true, IsPlayer: true });

        foreach (var teammate in teammates)
        {
            await ConsumeWithAndSuspect(choiceContext, teammate, creature);

            if (giftCount <= 0 || giftCandidates.Length == 0 || teammate.Player is not { } player)
                continue;

            for (var i = 0; i < giftCount; i++)
            {
                var canonical = rng.NextItem(giftCandidates);
                if (canonical is null) continue;

                var gift = CombatState.CreateCard(canonical, player);
                gift.SetToFreeThisCombat();
                await CardPileCmd.AddGeneratedCardToCombat(gift, PileType.Draw, player, CardPilePosition.Random);
            }
        }
    }

    private async Task ConsumeWithAndSuspect(
        PlayerChoiceContext choiceContext,
        Creature teammate,
        Creature applier)
    {
        if (teammate.GetPower<WithPower>() is { Amount: > 0 } withPower)
        {
            var withToRemove = Math.Min(WithLoss, (int)withPower.Amount);
            await PowerCmd.ModifyAmount(choiceContext, withPower, -withToRemove, applier, this, false);
        }

        if (teammate.GetPower<SuspectPower>() is { Amount: > 0 } suspectPower)
        {
            var suspectToRemove = Math.Min(SuspectLoss, (int)suspectPower.Amount);
            await PowerCmd.ModifyAmount(choiceContext, suspectPower, -suspectToRemove, applier, this, false);
        }
    }

    private IEnumerable<CardModel> GetRecordedGiftCandidates()
    {
        var sketchbook = Owner.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        if (sketchbook is null) yield break;

        var seenIds = new HashSet<ModelId>();
        foreach (var pool in sketchbook.GetRecordedCardPools())
        {
            foreach (var card in sketchbook.GetRecordableCardsFromPool(pool))
            {
                if (seenIds.Add(card.Id))
                    yield return card;
            }
        }
    }

    private int CountOtherCardPoolCardsPlayedThisCombat()
    {
        return CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
            entry.CardPlay.Card.Owner == Owner
            && entry.CardPlay.Card != this
            && entry.CardPlay.Card.Pool is not AnanlinCardPool);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
