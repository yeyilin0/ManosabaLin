using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>烧焦的痕迹 - 1费攻击, 反驳附魔, 10伤害+全体3伤害, 升级对单+4伤</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class BurntMarks : ManosabaEmalinCardTemplate
{
    public BurntMarks() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.RebuttalKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new DamageVar("AoEDamage", 3m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await DamageCmd.Attack(DynamicVars["AoEDamage"].BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}