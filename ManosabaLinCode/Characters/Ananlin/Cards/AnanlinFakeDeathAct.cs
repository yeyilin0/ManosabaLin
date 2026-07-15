using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinFakeDeathAct()
    : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<CrimsonbutterflyPower>(),
        HoverTipFactory.FromCard<BlankPage>(),
        HoverTipFactory.FromCard<MarginPage>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var peace = Owner.Creature.GetPower<AnanlinPeaceOfMindPower>();
        var lostPeace = Math.Max(0, (int)(peace?.Amount ?? 0));
        if (peace is { Amount: > 0 })
            await PowerCmd.ModifyAmount(choiceContext, peace, -peace.Amount, Owner.Creature, this);

        await PowerCmd.Apply<CrimsonbutterflyPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);

        var power = await PowerCmd.Apply<AnanlinFakeDeathActPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        if (power is null) return;

        power.LostPeace = lostPeace;
        power.RewardMarginPagesOnTrigger = IsUpgraded;
    }
}
