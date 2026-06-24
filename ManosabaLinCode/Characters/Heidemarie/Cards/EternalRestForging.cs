using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class EternalRestForging()
    : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    public const string AuroraSwordCountVar = "AuroraSwordCount";

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(AuroraSwordCountVar, 1),
        new PowerVar<EternalRestForgingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EternalRestForgingPower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<EternalRestForgingPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["EternalRestForgingPower"].BaseValue,
            Owner.Creature,
            this,
            false);

        power?.AddLayer((int)DynamicVars[AuroraSwordCountVar].BaseValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[AuroraSwordCountVar].UpgradeValueBy(1m);
    }
}
