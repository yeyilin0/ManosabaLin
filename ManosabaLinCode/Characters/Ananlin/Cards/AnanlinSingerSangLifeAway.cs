using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinSingerSangLifeAway()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    private const string SilencePerTriggerKey = "SilencePerTrigger";
    private const string LowHpThresholdKey = "LowHpThreshold";
    private const string BonusTriggersKey = "BonusTriggers";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>(SilencePerTriggerKey, 3m),
        new CardsVar(1),
        new EnergyVar(1),
        new DamageVar(6m, ValueProp.Move),
        new IntVar(LowHpThresholdKey, 10m),
        new IntVar(BonusTriggersKey, 2m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var silence = Owner.Creature.GetPower<SilentPower>();
        var consumed = Math.Max(0, (int)(silence?.Amount ?? 0));
        if (consumed <= 0) return;

        await PowerCmd.ModifyAmount(choiceContext, silence!, -consumed, Owner.Creature, this);

        var hpLoss = Math.Min(consumed, Math.Max(0, Owner.Creature.CurrentHp - 1));
        if (hpLoss > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                Owner.Creature,
                hpLoss,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                this,
                cardPlay);
        }

        var triggers = consumed / DynamicVars[SilencePerTriggerKey].IntValue;
        if (Owner.Creature.CurrentHp <= DynamicVars[LowHpThresholdKey].IntValue)
            triggers += DynamicVars[BonusTriggersKey].IntValue;

        for (var i = 0; i < triggers; i++)
            await TriggerSilentVocal(choiceContext, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[LowHpThresholdKey].UpgradeValueBy(20m);
    }

    private async Task TriggerSilentVocal(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);

        foreach (var enemy in CombatState.HittableEnemies.ToArray())
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                DynamicVars.Damage.BaseValue,
                DynamicVars.Damage.Props,
                Owner.Creature,
                this,
                cardPlay);
        }
    }
}
