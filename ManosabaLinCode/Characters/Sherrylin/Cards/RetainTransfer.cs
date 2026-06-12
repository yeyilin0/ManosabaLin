using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力转移：选择手牌中1张带保留计数组件的卡，将其计数转移给另1张带件的卡，升级原卡获得消耗
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainTransfer() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private LocString SelectionScreenPrompt2 => new("cards", Id.Entry + ".selectionScreenPrompt2");
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var retainCards = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source && c.HasComponent<RetainCounterComponent>()).ToList();

        if (retainCards.Count < 2) return;

        // 选择源卡
        var prefs1 = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected1 = await CardSelectCmd.FromSimpleGrid(choiceContext, retainCards, source.Owner, prefs1);
        var sourceList = selected1.ToList();
        if (sourceList.Count == 0) return;

        var sourceCard = sourceList[0];
        var sourceComp = sourceCard is MinionLib.Component.Interfaces.IComponentsCardModel ccm ? ccm.Components.OfType<RetainCounterComponent>().FirstOrDefault() : null;
        if (sourceComp == null) return;

        var sourceCounterField = typeof(RetainCounterComponent).GetField("_counter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sourceCounterField == null) return;

        var counter = (int)sourceCounterField.GetValue(sourceComp);
        if (counter <= 1) return;

        // 选择目标卡
        var targets = retainCards.Where(c => c != sourceCard).ToList();
        if (targets.Count == 0) return;

        var prefs2 = new CardSelectorPrefs(SelectionScreenPrompt2, 1);
        var selected2 = await CardSelectCmd.FromSimpleGrid(choiceContext, targets, source.Owner, prefs2);
        var targetList = selected2.ToList();
        if (targetList.Count == 0) return;

        var targetCard = targetList[0];
        var targetComp = targetCard is MinionLib.Component.Interfaces.IComponentsCardModel ccm2 ? ccm2.Components.OfType<RetainCounterComponent>().FirstOrDefault() : null;
        if (targetComp == null) return;

        var targetCounterField = typeof(RetainCounterComponent).GetField("_counter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (targetCounterField == null) return;

        // 转移计数
        var targetCounter = (int)targetCounterField.GetValue(targetComp);
        targetCounterField.SetValue(targetComp, targetCounter + counter - 1);
        sourceCounterField.SetValue(sourceComp, 1);

        // 升级时原卡获得消耗
        if (IsUpgraded)
            sourceCard.AddKeyword(MegaCrit.Sts2.Core.Entities.Cards.CardKeyword.Exhaust);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
