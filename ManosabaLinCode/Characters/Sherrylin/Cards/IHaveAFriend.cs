using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 吾有一友：选择一张额外牌组的卡给一个队友，如果没有队友则获得吾即吾友能力，升级减一费。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class IHaveAFriend() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<IAmMyOwnFriendPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var combatState = source.CombatState;
        if (combatState == null) return;

        // 获取队友
        var teammates = combatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c is { IsAlive: true, IsPlayer: true })
            .ToList();

        if (teammates.Count > 0)
        {
            // 有队友：选择一张额外牌组的卡给队友
            var caseFileCards = MainFile.CaseFilePile.GetPile(source.Owner).Cards.ToList();
            if (caseFileCards.Count == 0) return;

            // 选择要给的卡
            var cardPrefs = new CardSelectorPrefs(new LocString("selectionScreenPrompt", "选择一张卡给予队友"), 1);
            var cardSelection = await CardSelectCmd.FromSimpleGrid(
                choiceContext, caseFileCards, source.Owner, cardPrefs);
            var selectedCard = cardSelection.FirstOrDefault();
            if (selectedCard == null) return;

            // 选择要给的队友
            var target = teammates[0];
            if (teammates.Count > 1)
            {
                // 如果有多个队友，让玩家选择（这里简化为选第一个）
                target = teammates[0];
            }

            // 移动卡到队友
            await CardPileCmd.RemoveFromCombat(selectedCard);
            if (target.Player != null)
                await CardPileCmd.Add(selectedCard, MainFile.CaseFilePile, CardPilePosition.Top);
        }
        else
        {
            // 没有队友：获得吾即吾友能力
            await PowerCmd.Apply<IAmMyOwnFriendPower>(
                choiceContext, source.Owner.Creature, 1,
                source.Owner.Creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
