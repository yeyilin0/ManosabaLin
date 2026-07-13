using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinGentleNod()
    : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new IntVar("ExtraPlays", 1),
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

        var peace = this.PeaceOfMindAmount();
        if (peace > 0)
        {
            await this.PullMatchingCardsToHand(
                choiceContext,
                peace * DynamicVars.Cards.IntValue,
                card => AnanlinCardHelpers.IsPlayableCombatCard(card) && card.Type != CardType.Attack);
            await this.LosePeaceOfMind(choiceContext);
            this.Sketchbook()?.QueueNonAttackRepeatThisTurn(DynamicVars["ExtraPlays"].IntValue);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["ExtraPlays"].UpgradeValueBy(1m);
    }
}
