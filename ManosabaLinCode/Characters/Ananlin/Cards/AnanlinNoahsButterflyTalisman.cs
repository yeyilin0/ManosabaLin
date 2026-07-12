using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinNoahsButterflyTalisman() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string RecordVar = "Record";
    private const string TriggerSilenceVar = "TriggerSilence";
    private const string FallbackSilenceVar = "FallbackSilence";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<AnanlinButterflyTalismanPower>(RecordVar, 8m),
        new PowerVar<SilentPower>(TriggerSilenceVar, 2m),
        new PowerVar<SilentPower>(FallbackSilenceVar, 4m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinButterflyTalismanPower>(),
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<CrimsonbutterflyPower>()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target is not { } target) return;

        if (HasAttackIntent(target))
        {
            var talisman = await PowerCmd.Apply<AnanlinButterflyTalismanPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[RecordVar].BaseValue,
                Owner.Creature,
                this);
            talisman?.Arm(target, DynamicVars[TriggerSilenceVar].IntValue);
            return;
        }

        if (this.Sketchbook() is { } sketchbook)
            await sketchbook.AddSilence(choiceContext, DynamicVars[FallbackSilenceVar].IntValue, this);
        else
            await PowerCmd.Apply<SilentPower>(
                choiceContext,
                Owner.Creature,
                DynamicVars[FallbackSilenceVar].BaseValue,
                Owner.Creature,
                this);

        var returnPower = await PowerCmd.Apply<AnanlinDelayedCardReturnPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        returnPower?.AddCard(this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[RecordVar].UpgradeValueBy(4m);
    }

    private bool HasAttackIntent(Creature target)
    {
        if (this.Sketchbook() is { } sketchbook)
            return sketchbook.IsAttackIntent(target);

        return target.Monster?.NextMove?.Intents.Any(static intent => intent is AttackIntent) == true;
    }
}
