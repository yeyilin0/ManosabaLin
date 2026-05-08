using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Attacksixsix : ManosabaCardTemplate
{
    private const int RequiredAttackCards = 3;

    public Attacksixsix()
        : base(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<JudgementChainPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move)
    ];

    private int GetAttackCardsPlayedThisTurn()
    {
        return CombatManager.Instance?.History.Entries
            .OfType<CardPlayFinishedEntry>()
            .Count(e => e.HappenedThisTurn(CombatState)
                        && e.CardPlay.Card.Owner == Owner
                        && e.CardPlay.Card.Type == CardType.Attack) ?? 0;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target;

        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (GetAttackCardsPlayedThisTurn() >= RequiredAttackCards)
        {
            await PowerCmd.Apply<JudgementChainPower>(
                choiceContext,
                source.Owner.Creature,
                1m,
                source.Owner.Creature,
                source,
                false
            );
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }
}