using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinUnnoticed()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(6m, ValueProp.Move),
        new CardsVar(1)
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
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        var bonusDraws = this.PeaceOfMindAmount() * DynamicVars.Cards.IntValue;
        if (!HasAnyEnemyAttackIntent())
        {
            await this.GainPeaceOfMind(choiceContext);
            if (bonusDraws > 0)
                await CardPileCmd.Draw(choiceContext, bonusDraws, Owner);
            return;
        }

        var power = await PowerCmd.Apply<AnanlinUnnoticedPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        power?.Arm(this, bonusDraws);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    private bool HasAnyEnemyAttackIntent()
    {
        return this.Sketchbook()?.HasAnyEnemyAttackIntent() == true;
    }
}
