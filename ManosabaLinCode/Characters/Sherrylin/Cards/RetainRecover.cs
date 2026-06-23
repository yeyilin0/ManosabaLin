using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力回收：选弃牌堆一张保留计数组件卡移回手卡并使其蓄力计数加1，升级计数每次加2
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainRecover() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => RetainCounterComponent.Tip;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var discardPile = PileType.Discard.GetPile(source.Owner).Cards
            .Where(c => c.HasComponent<RetainCounterComponent>()).ToList();

        if (discardPile.Count == 0) return;

        var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, discardPile, source.Owner, prefs);
        var selectedList = selected.ToList();
        if (selectedList.Count > 0)
        {
            var card = selectedList[0];
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);

            var component = (card as IComponentsCardModel)?.Components.OfType<RetainCounterComponent>().FirstOrDefault();
            if (component != null)
            {
                var counterField = typeof(RetainCounterComponent).GetField("_counter",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (counterField != null)
                {
                    var current = (int)counterField.GetValue(component);
                    counterField.SetValue(component, current + (IsUpgraded ? 2 : 1));
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
