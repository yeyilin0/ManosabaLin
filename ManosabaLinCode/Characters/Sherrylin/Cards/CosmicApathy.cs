using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 宇宙冷漠：消耗情绪直到只剩一层，随机攻击全场目标1点生命，
/// 攻击到敌人使敌人获得受到冷漠能力，攻击到友方获得10层情绪，打出移除，升级减一费。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class CosmicApathy() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionPower>();
            yield return HoverTipFactory.FromPower<CosmicApathyDebuffPower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获取情绪能力
        var emotionPower = source.Owner.Creature.GetPower<EmotionPower>();
        if (emotionPower == null) return;

        // 消耗情绪直到只剩1层
        var consumeCount = (int)emotionPower.Amount - 1;
        if (consumeCount <= 0) return;

        emotionPower.Amount = 1;
        emotionPower.Flash();

        // 收集所有目标（友方+敌方）
        var combatState = source.CombatState;
        if (combatState == null) return;

        var allTargets = combatState.Allies.Concat(combatState.Enemies)
            .Where(c => c is { IsAlive: true })
            .ToList();

        if (allTargets.Count == 0) return;

        var rng = source.Owner.RunState.Rng.CombatTargets;

        // 每消耗1层，随机攻击一个目标1点伤害
        for (int i = 0; i < consumeCount; i++)
        {
            var target = rng.NextItem(allTargets);

            await CreatureCmd.Damage(choiceContext, target, 1m,
                ValueProp.Unpowered, source.Owner.Creature, source);

            // 判断目标是敌方还是友方
            if (target.Side != source.Owner.Creature.Side)
            {
                // 攻击到敌人：施加受到冷漠能力
                var existingDebuff = target.GetPower<CosmicApathyDebuffPower>();
                if (existingDebuff != null)
                {
                    existingDebuff.Amount++;
                    existingDebuff.Applier = source.Owner.Creature;
                    existingDebuff.Flash();
                }
                else
                {
                    await PowerCmd.Apply<CosmicApathyDebuffPower>(
                        choiceContext, target, 1,
                        source.Owner.Creature, source, false);

                    var debuff = target.GetPower<CosmicApathyDebuffPower>();
                    if (debuff != null)
                        debuff.Applier = source.Owner.Creature;
                }
            }
            else
            {
                // 攻击到友方：获得10层情绪
                await PowerCmd.Apply<EmotionPower>(
                    choiceContext, source.Owner.Creature, 10,
                    source.Owner.Creature, source, false);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
