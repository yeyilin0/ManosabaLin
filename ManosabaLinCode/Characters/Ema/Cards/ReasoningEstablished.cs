using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>推理成立 - 1费攻击, 每打过赞同反驳疑问附魔牌+2伤害, 升级基础+3且每张+3</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class ReasoningEstablished : ManosabaCardTemplate
{
    public ReasoningEstablished() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var enchantmentCount = EmalinCombatHelper.GetTotalEnchantmentPlaysThisTurn(Owner.Creature, CombatState);
        var bonusPerCard = IsUpgraded ? 3m : 2m;
        var totalDamage = DynamicVars.Damage.BaseValue + enchantmentCount * bonusPerCard;

        await DamageCmd.Attack(totalDamage)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
