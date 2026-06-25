using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class LiberatedAurora()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public const string CondensedAurorasKey = "CondensedAuroras";

    private const decimal DamagePerConsumedAuroraSword = 1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move),
        new CardsVar(CondensedAurorasKey, 1)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target == null)
            return;

        var consumedAuroraSwords = await AuroraSuiteHelper.ExhaustAuroraSwordsFromCombatPiles(
            choiceContext,
            Owner);
        var damage = DynamicVars.Damage.BaseValue + consumedAuroraSwords * DamagePerConsumedAuroraSword;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this))
            return;

        await AuroraSuiteHelper.AddCondensedAurorasToHand(
            Owner,
            DynamicVars[CondensedAurorasKey].IntValue,
            this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars[CondensedAurorasKey].UpgradeValueBy(1m);
    }
}
