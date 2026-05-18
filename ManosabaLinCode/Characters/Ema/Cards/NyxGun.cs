using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>奈叶香的枪 - 2费攻击, 反驳附魔, 15伤害, 反驳≥3额外命中一个敌人, 升级+5伤</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class NyxGun : ManosabaEmalinCardTemplate
{
    public NyxGun() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.RebuttalKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(15m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var rebuttalCount = EmalinCombatHelper.GetRebuttalPlaysThisTurn(Owner.Creature, CombatState);
        if (rebuttalCount >= 3)
        {
            var otherEnemies = CombatState.HittableEnemies
                .Where(e => e != cardPlay.Target && e.IsAlive).ToList();
            if (otherEnemies.Count > 0)
            {
                var extraTarget = Owner.RunState.Rng.CombatTargets.NextItem(otherEnemies);
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(extraTarget)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}