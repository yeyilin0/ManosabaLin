using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>弩枪的箭矢 - 1费攻击, 反驳附魔, 5伤害, 攻击意图翻倍, 升级+3伤</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class CrossbowBolt : ManosabaEmalinCardTemplate
{
    public CrossbowBolt() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.RebuttalKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target!;
        bool isAttackIntent = target.Monster?.NextMove?.Intents
            .Any(i => i.IntentType == IntentType.Attack || i.IntentType == IntentType.DeathBlow) ?? false;
        var damage = isAttackIntent ? DynamicVars.Damage.BaseValue * 2 : DynamicVars.Damage.BaseValue;

        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}