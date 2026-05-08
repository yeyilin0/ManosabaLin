using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
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
public sealed class ShadowSlash : ManosabaCardTemplate
{
    private const string BonusDamageKey = "BonusDamage";

    public ShadowSlash()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [TransmigrationRules.TransmigrationKeywordId];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerjuryPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4m, ValueProp.Move),
        new DynamicVar(BonusDamageKey, 4m)
    ];

    private bool HasTransmigrationPlayedThisTurn()
    {
        return CombatManager.Instance?.History.Entries
            .OfType<CardPlayFinishedEntry>()
            .Any(e => e.HappenedThisTurn(CombatState)
                      && e.CardPlay.IsAutoPlay
                      && TransmigrationRules.HasTransmigration(e.CardPlay.Card)) ?? false;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target;

        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 基础伤害（受力量影响）
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 追加伤害（不受力量影响）
        if (HasTransmigrationPlayedThisTurn())
        {
            await DamageCmd.Attack(source.DynamicVars[BonusDamageKey].BaseValue)
                .FromCard(source)
                .Targeting(target)
                .Unpowered()
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars[BonusDamageKey].UpgradeValueBy(2m);
    }
}