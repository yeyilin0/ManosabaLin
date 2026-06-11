using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力增幅：带保留计数组件，使手里随机保留计数组件计数增加1，打出移除，升级变成消耗
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainAmplify() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RetainCounterComponent()];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            if (IsUpgraded)
                yield return CardKeyword.Exhaust;
            else
                yield return CardKeyword.Exhaust;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var retainCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source && c.HasComponent<RetainCounterComponent>()).ToList();

        if (retainCards.Count > 0)
        {
            var rng = source.Owner.RunState.Rng.CombatCardSelection;
            var target = retainCards[rng.NextInt(retainCards.Count)];
            var component = target is MinionLib.Component.Interfaces.IComponentsCardModel ccm ? ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault() : null;
            if (component != null)
            {
                // 手动增加计数 - 通过反射或直接访问
                // 由于组件是 partial class，我们需要通过 DynamicVars 来影响
                // 这里简单地让组件再触发一次增长
                var counterField = typeof(RetainCounterComponent).GetField("_counter",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (counterField != null)
                {
                    var current = (int)counterField.GetValue(component);
                    counterField.SetValue(component, current + (IsUpgraded ? 2 : 1));
                }
            }
        }

        // 打出移除（非升级时）
        if (!IsUpgraded)
            await CardPileCmd.RemoveFromCombat(source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
