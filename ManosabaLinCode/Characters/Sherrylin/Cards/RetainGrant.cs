using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 蓄力赋予：使一张手卡获得保留计数组件，升级减一费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class RetainGrant() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var hand = PileType.Hand.GetPile(source.Owner).Cards
            .Where(c => c != source && !c.HasComponent<RetainCounterComponent>()).ToList();

        if (hand.Count > 0)
        {
            var prefs = new MegaCrit.Sts2.Core.CardSelection.CardSelectorPrefs(
                new MegaCrit.Sts2.Core.Localization.LocString("RetainGrant", "选择一张卡获得保留计数"), 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, hand, source.Owner, prefs);
            var selectedList = selected.ToList();
            if (selectedList.Count > 0)
            {
                selectedList[0].TryAddComponent(new RetainCounterComponent());
                selectedList[0].AddKeyword(CardKeyword.Retain);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
