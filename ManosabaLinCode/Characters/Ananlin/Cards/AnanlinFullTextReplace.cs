using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinFullTextReplace()
    : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12m, ValueProp.Move),
        new PowerVar<SilentPower>("Silence", 2m),
        new BlockVar(3m, ValueProp.Move),
        new IntVar("RewriteStep", 4),
        new CardsVar("CardsPerBlankPage", 2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromCard<BlankPage>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            BuildNameOptions(),
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1))).FirstOrDefault();
        if (selected is null) return;

        var toExhaust = GetCardsInMainCombatPiles()
            .Where(card => card.Id == selected.Id)
            .ToArray();
        if (toExhaust.Length == 0) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var exhausted = 0;
        foreach (var card in toExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
            exhausted++;
        }

        await this.AddSilence(choiceContext, exhausted * DynamicVars["Silence"].IntValue);
        await CreatureCmd.GainBlock(Owner.Creature, exhausted * DynamicVars.Block.BaseValue, ValueProp.Move, cardPlay);

        var rewriteStep = DynamicVars["RewriteStep"].IntValue;
        var rewriteCount = exhausted / rewriteStep;

        for (var i = 0; i < rewriteCount; i++)
            if (this.Sketchbook() is { } sketchbook)
                await sketchbook.TriggerSilenceRewrite(choiceContext);

        var pageCount = exhausted / DynamicVars["CardsPerBlankPage"].IntValue;
        for (var i = 0; i < pageCount; i++)
            await this.AddBlankPageToHand(true);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private IReadOnlyList<CardModel> BuildNameOptions()
    {
        var seen = new HashSet<ModelId>();
        return GetCardsInMainCombatPiles()
            .Where(card => seen.Add(card.Id))
            .OrderBy(card => card.Title.ToString())
            .ToArray();
    }

    private IEnumerable<CardModel> GetCardsInMainCombatPiles()
    {
        return PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards);
    }
}
