using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class CrimsonSword : ManosabaCardTemplate, ICrimsonSwordCard
{
    private const decimal BaseDamage = 1m;
    private const decimal UpgradeDamage = 1m;
    private const decimal BaseAuroraChainGain = 1m;
    private const decimal UpgradeAuroraChainGain = 1m;

    private bool _discardedFromHandByDiscardCommand;

    public CrimsonSword() : base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
    {
        RegisterTokenFactory();
    }

    #pragma warning disable CA2255
    [System.Runtime.CompilerServices.ModuleInitializer]
    #pragma warning restore CA2255
    public static void RegisterTokenFactory()
    {
        SwordTokenGeneration.RegisterTokenCard<CrimsonSword>(SwordTokenKind.Crimson);
    }

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new SwordGraveComponent(),
        new LinkComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move),
        new DynamicVar("AuroraChain", BaseAuroraChainGain)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (cardPlay.Target == null)
            return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? source,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this))
            return Task.CompletedTask;

        _discardedFromHandByDiscardCommand =
            oldPileType == PileType.Hand && card.Pile?.Type == PileType.Discard;

        return Task.CompletedTask;
    }

    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this) || !_discardedFromHandByDiscardCommand)
            return;

        _discardedFromHandByDiscardCommand = false;
        await PowerCmd.Apply<AuroraChainPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["AuroraChain"].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(UpgradeDamage);
        DynamicVars["AuroraChain"].UpgradeValueBy(UpgradeAuroraChainGain);
    }
}
