using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力释放：手牌中所有带保留计数组件的卡：计数归零，每归零1点计数获得1能量，升级获得保留
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainBurst() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => RetainCounterComponent.Tip;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var retainCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c.HasComponent<RetainCounterComponent>()).ToList();

        int totalEnergy = 0;

        foreach (var card in retainCards)
        {
            if (card is not IComponentsCardModel ccm) continue;
            var comp = ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (comp == null) continue;

            totalEnergy += comp.Counter;

            // 重置计数器为 1
            var counterField = typeof(RetainCounterComponent).GetField("_counter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            counterField?.SetValue(comp, 1);
        }

        if (totalEnergy > 0)
            await PlayerCmd.GainEnergy(totalEnergy, source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Retain);
    }
}
