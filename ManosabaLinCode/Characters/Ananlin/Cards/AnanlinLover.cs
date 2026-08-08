using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinLover() : ManosabaCardTemplate(4, CardType.Power, CardRarity.Ancient, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_ANANLIN_LOVER_EFFECT";
    private const int MaxGeneratedCards = 3;
    private const string BlockPerSilenceKey = "BlockPerSilence";
    private const string MaxCardsKey = "MaxCards";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(100m),
        new PowerVar<RitualCeremonyPower>(1m),
        new PowerVar<AnanlinPeaceOfMindPower>(1m),
        new IntVar(BlockPerSilenceKey, 2m),
        new IntVar(MaxCardsKey, MaxGeneratedCards)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<WithPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["WithPower"].BaseValue,
            Owner.Creature,
            this,
            false);

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["RitualCeremonyPower"].BaseValue,
            Owner.Creature,
            this,
            false);

        var sketchbook = this.Sketchbook();
        if (sketchbook is not null)
        {
            await this.GainPeaceOfMind(choiceContext, sketchbook.RecordedPoolCount);
            await GenerateRareCardsFromRecordedPools(choiceContext, sketchbook);

            var block = sketchbook.CurrentSilence * DynamicVars[BlockPerSilenceKey].IntValue;
            if (block > 0)
                await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private async Task GenerateRareCardsFromRecordedPools(PlayerChoiceContext choiceContext, AnansSketchbook sketchbook)
    {
        if (CombatState is not { } combatState) return;

        var generated = RollRarePlayableFromEachRecordedPool(sketchbook, combatState);
        foreach (var card in generated)
        {
            if (await this.LosePeaceOfMind(choiceContext) > 0)
            {
                card.SetFreeIgnoringCardPlayConditions();
                card.GetOrCreateCapability<AnanlinLoverDoublePlayCapability>();
            }
            else
            {
                card.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
            }

            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }

    private IReadOnlyList<CardModel> RollRarePlayableFromEachRecordedPool(
        AnansSketchbook sketchbook,
        ICombatState combatState)
    {
        var cards = new List<CardModel>();
        var usedIds = new HashSet<ModelId>();
        var simulatedPeace = this.PeaceOfMindAmount();

        foreach (var pool in sketchbook.GetRecordedCardPools().Take(MaxGeneratedCards))
        {
            var willSpendPeace = simulatedPeace > 0;
            var card = RollRarePlayableFromPool(sketchbook, pool, combatState, willSpendPeace, usedIds);
            if (card is null) continue;

            cards.Add(card);
            usedIds.Add(card.Id);

            if (willSpendPeace)
                simulatedPeace--;
        }

        return cards;
    }

    private CardModel? RollRarePlayableFromPool(
        AnansSketchbook sketchbook,
        CardPoolModel pool,
        ICombatState combatState,
        bool willSpendPeace,
        ISet<ModelId> usedIds)
    {
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var candidates = sketchbook
            .GetRecordableCardsFromPool(pool)
            .Where(static template => template.Rarity == CardRarity.Rare)
            .Where(static template => !template.EnergyCost.CostsX)
            .Where(template => !usedIds.Contains(template.Id))
            .OrderBy(_ => rng.NextFloat())
            .ToArray();

        foreach (var template in candidates)
        {
            var probe = combatState.CreateCard(template, Owner);
            ApplyPlayabilityProbeModifier(probe, willSpendPeace);
            if (!IsCurrentlyPlayable(probe, combatState)) continue;

            return combatState.CreateCard(template, Owner);
        }

        return null;
    }

    private static void ApplyPlayabilityProbeModifier(CardModel card, bool willSpendPeace)
    {
        if (willSpendPeace)
            card.SetFreeIgnoringCardPlayConditions();
        else
            card.EnergyCost.AddThisTurnOrUntilPlayed(-1, reduceOnly: true);
    }

    private static bool IsCurrentlyPlayable(CardModel card, ICombatState combatState)
    {
        return AnanlinCardHelpers.HasValidEffectTarget(card, combatState);
    }
}
