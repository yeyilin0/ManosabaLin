using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 和笨蛋对视：选择一个敌人，如果意图是攻击则给予虚弱，否则给予易伤，升级增加层数。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class StareDownFool() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 检查敌人意图
        var intents = target.Monster?.NextMove?.Intents;
        bool isAttackIntent = false;

        if (intents != null)
        {
            foreach (var intent in intents)
            {
                if (intent.IntentType == IntentType.Attack || intent.IntentType == IntentType.DeathBlow)
                {
                    isAttackIntent = true;
                    break;
                }
            }
        }

        if (isAttackIntent)
        {
            // 意图是攻击：给予虚弱
            await PowerCmd.Apply<WeakPower>(
                choiceContext, target,
                source.DynamicVars.Weak.BaseValue,
                source.Owner.Creature, source, false);
        }
        else
        {
            // 意图不是攻击：给予易伤
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext, target,
                source.DynamicVars.Vulnerable.BaseValue,
                source.Owner.Creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}
