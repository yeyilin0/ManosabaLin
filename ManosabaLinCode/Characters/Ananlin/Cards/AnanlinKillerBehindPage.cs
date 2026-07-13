namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinKillerBehindPage()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromCard<BlankPage>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        var toExhaust = GetCardsInMainCombatPiles()
            .Where(card => !AnanlinCardHelpers.IsAnanlinPoolCard(card))
            .ToArray();
        var hits = 1 + toExhaust.Length;

        for (var i = 0; i < hits; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .Targeting(target)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(choiceContext);
        }

        foreach (var card in toExhaust)
        {
            await CardCmd.Exhaust(choiceContext, card);
            await this.AddBlankPageToDrawPile(false);
        }

        if (toExhaust.Length == 0) return;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        var currentEnergy = Owner.PlayerCombatState.Energy;
        var maxEnergy = Owner.PlayerCombatState.MaxEnergy;
        var energyToGain = maxEnergy - currentEnergy;
        if (energyToGain > 0)
            await PlayerCmd.GainEnergy(energyToGain, Owner);
    }

    private IEnumerable<CardModel> GetCardsInMainCombatPiles()
    {
        return PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards);
    }
}
