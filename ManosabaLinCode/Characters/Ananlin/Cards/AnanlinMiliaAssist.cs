using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinMiliaAssist()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var selected = await SelectHandCard(choiceContext);
        if (selected is null) return;

        var replacement = RollRecordedReplacement(selected);
        if (replacement is null) return;

        await CardCmd.Transform(selected, replacement);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(1);
    }

    private async Task<CardModel?> SelectHandCard(PlayerChoiceContext choiceContext)
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards
            .Where(CanTransformSource)
            .ToArray();
        if (candidates.Length == 0) return null;

        return (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1, 1))).FirstOrDefault();
    }

    private CardModel? RollRecordedReplacement(CardModel source)
    {
        if (this.Sketchbook() is not { } sketchbook) return null;
        if (CombatState is not { } combatState) return null;

        var candidates = BuildRecordedReplacements(sketchbook, source, combatState).ToArray();
        return candidates.Length == 0
            ? null
            : Owner.RunState.Rng.CombatCardGeneration.NextItem(candidates);
    }

    private IEnumerable<CardModel> BuildRecordedReplacements(
        AnansSketchbook sketchbook,
        CardModel source,
        ICombatState combatState)
    {
        var seenIds = new HashSet<ModelId>();

        foreach (var pool in sketchbook.GetRecordedCardPools())
        {
            foreach (var template in sketchbook.GetRecordableCardsFromPool(pool))
            {
                if (!seenIds.Add(template.Id)) continue;
                if (!CanUseReplacementTemplate(template, source)) continue;

                var replacement = combatState.CreateCard(template, Owner);
                sketchbook.CopyVisibleAdditions(source, replacement);
                ApplyReplacementModifiers(replacement);

                if (CanGuaranteePlayable(replacement, combatState))
                    yield return replacement;
            }
        }
    }

    private void ApplyReplacementModifiers(CardModel replacement)
    {
        if (IsUpgraded)
        {
            replacement.SetFreeIgnoringCardPlayConditions();
            replacement.GetOrCreateCapability<AnanlinAuditionPeaceEnergyCapability>();
            return;
        }

        replacement.GetOrCreateCapability<AnanlinAuditionPeaceCapability>();
    }

    private static bool CanTransformSource(CardModel card)
    {
        return card.IsTransformable
            && AnanlinCardHelpers.IsPlayableCombatCard(card)
            && card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    }

    private static bool CanUseReplacementTemplate(CardModel template, CardModel source)
    {
        return template.Id != source.Id
            && !template.EnergyCost.CostsX
            && AnanlinCardHelpers.IsPlayableCombatCard(template)
            && template.Type is CardType.Attack or CardType.Skill or CardType.Power
            && AnansSketchbook.CanSketchbookGenerate(template);
    }

    private static bool CanGuaranteePlayable(CardModel card, ICombatState combatState)
    {
        return AnanlinCardHelpers.HasValidEffectTarget(card, combatState)
            && card.CanPlay();
    }
}
