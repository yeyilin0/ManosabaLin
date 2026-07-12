namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinWitchPower()
    : ManosabaCardTemplate(4, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    private const int RequiredWithAmount = 100;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(RequiredWithAmount),
        new PowerVar<IntangiblePower>(1m),
        new PowerVar<RitualCeremonyPower>(2m),
        new PowerVar<VoidFormPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<IntangiblePower>(),
        HoverTipFactory.FromPower<RitualCeremonyPower>(),
        HoverTipFactory.FromPower<VoidFormPower>()
    ];

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            return Owner.Creature.GetPower<WithPower>()?.Amount >= RequiredWithAmount;
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<IntangiblePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["IntangiblePower"].BaseValue,
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

        await PowerCmd.Apply<VoidFormPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["VoidFormPower"].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
