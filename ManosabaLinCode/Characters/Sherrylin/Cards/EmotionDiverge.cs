using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 绪分两途：如果情绪层数低于六层则攻击敌人，如果高于则获得护盾，
/// 升级有两个效果的同时额外获得等于友方数量的情绪层数
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionDiverge() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new BlockVar(10m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        var emotionPower = source.Owner.Creature.GetPower<EmotionPower>();
        var emotionAmount = emotionPower?.Amount ?? 0;

        if (IsUpgraded)
        {
            // 升级：无论大于6还是小于6都触发对应效果，并额外获得情绪
            if (emotionAmount < 6)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    target,
                    source.DynamicVars.Damage.BaseValue,
                    ValueProp.Move,
                    source.Owner.Creature,
                    source);
            }
            else
            {
                await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);
            }

            // 额外获得等于友方数量的情绪层数
            var allyCount = CombatState.Allies.Count(a => a is { IsAlive: true });
            if (allyCount > 0)
            {
                await PowerCmd.Apply<EmotionPower>(
                    choiceContext, source.Owner.Creature, allyCount,
                    source.Owner.Creature, null, false);
            }
        }
        else if (emotionAmount < 6)
        {
            // 低于6层：攻击敌人
            await CreatureCmd.Damage(
                choiceContext,
                target,
                source.DynamicVars.Damage.BaseValue,
                ValueProp.Move,
                source.Owner.Creature,
                source);
        }
        else
        {
            // 6层及以上：获得护盾
            await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级改变行为逻辑（通过 IsUpgraded 判断），不改变数值
    }
}
