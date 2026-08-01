using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinNoahAssist() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string AssistPowerKey = "AnanlinNoahAssistPower";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinNoahAssistPower>(AssistPowerKey, 1m),
        new PowerVar<NymPower>(AnanlinNoahAssistPower.RewrittenNymPowerKey, 1m),
        new PowerVar<NymPower>(AnanlinNoahAssistPower.NoRewriteNymPowerKey, 3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinBrainwashPower>(),
        HoverTipFactory.FromPower<NymPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var assist = await PowerCmd.Apply<AnanlinNoahAssistPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars[AssistPowerKey].BaseValue,
            Owner.Creature,
            this);

        assist?.Configure(
            DynamicVars[AnanlinNoahAssistPower.RewrittenNymPowerKey].IntValue,
            DynamicVars[AnanlinNoahAssistPower.NoRewriteNymPowerKey].IntValue);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[AnanlinNoahAssistPower.RewrittenNymPowerKey].UpgradeValueBy(1m);
        DynamicVars[AnanlinNoahAssistPower.NoRewriteNymPowerKey].UpgradeValueBy(1m);
    }
}
