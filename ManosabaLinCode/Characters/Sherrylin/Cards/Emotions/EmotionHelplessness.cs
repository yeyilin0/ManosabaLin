using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Extensions;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

/// <summary>
/// 无助（情绪卡）：带蓄力组件和无助费用组件，打出时获得力量、减少敏捷、造成伤害、获得格挡（受蓄力计数加成），然后获得无助充能球。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class EmotionHelplessness() : CaseFileCard<EmotionHelplessnessOrb>(-1, CardRarity.Ancient, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent()];

    // 回合开始费用+1，费用≥4时获得升空组件
    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player, ComponentContext componentContext)
    {
        if (Owner != player) return;

        EnergyCost.AddThisCombat(1);

        if (EnergyCost.Canonical >= 4 && this is IComponentsCardModel ccm && !ccm.HasComponent<LevitationComponent>())
        {
            ccm.AddComponent(new LevitationComponent());
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        // 读取蓄力计数
        int counter = 1;
        if (source is IComponentsCardModel ccm)
        {
            var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (comp != null)
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var counterField = typeof(RetainCounterComponent).GetField("_counter", flags);
                if (counterField != null)
                    counter = (int)counterField.GetValue(comp);
            }
        }

        // 获得力量
        await PowerCmd.Apply<TempStrength>(
            choiceContext, source.Owner.Creature, counter * 2,
            source.Owner.Creature, source, false);

        // 减少敏捷
        await PowerCmd.Apply<TempStrengthDown>(
            choiceContext, source.Owner.Creature, counter * 2,
            source.Owner.Creature, source, false);

        // 造成伤害（受计数加成）
        var combatState = source.CombatState;
        if (combatState != null)
        {
            var enemies = combatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var target = enemies[rng.NextInt(enemies.Count)];
                await CreatureCmd.Damage(choiceContext, target, counter * 5, ValueProp.Move, source.Owner.Creature, source);
            }
        }

        // 获得格挡（受计数加成）
        await CreatureCmd.GainBlock(source.Owner.Creature, counter * 5, ValueProp.Move, cardPlay);

        // 获得充能球
        await base.OnPlay(choiceContext, cardPlay, componentContext);
    }
}
