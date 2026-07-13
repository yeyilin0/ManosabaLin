using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinPushTheDoorOpen()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    private const string PeaceBonusVar = "PeaceBonus";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(16m, ValueProp.Move),
        new IntVar(PeaceBonusVar, 3),
        new IntVar("Hits", 1)
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

        var peace = this.PeaceOfMindAmount();
        var damage = DynamicVars.Damage.BaseValue + peace * DynamicVars[PeaceBonusVar].IntValue;

        await DamageCmd.Attack(damage)
            .WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        if (peace <= 0 || CombatState is not { } combatState) return;
        if (await this.LosePeaceOfMind(choiceContext) <= 0) return;

        await DamageCmd.Attack(damage)
            .WithHitCount(DynamicVars["Hits"].IntValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(-8m);
        DynamicVars["Hits"].UpgradeValueBy(1m);
    }
}
