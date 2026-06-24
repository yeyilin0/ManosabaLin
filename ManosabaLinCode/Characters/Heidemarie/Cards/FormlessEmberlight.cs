using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class FormlessEmberlight()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public const string SwordCountKey = FormlessEmberlightPower.SwordCountKey;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(SwordCountKey, FormlessEmberlightPower.BaseSwordCount),
        new PowerVar<FormlessEmberlightPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<FormlessEmberlightPower>(); }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Innate;
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = Owner.Creature.GetPower<FormlessEmberlightPower>();
        if (power == null)
        {
            power = await PowerCmd.Apply<FormlessEmberlightPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[nameof(FormlessEmberlightPower)].BaseValue,
                Owner.Creature,
                this,
                false);
        }

        power?.Configure(DynamicVars[SwordCountKey].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[SwordCountKey].UpgradeValueBy(1m);
        AddKeyword(CardKeyword.Innate);
    }
}
