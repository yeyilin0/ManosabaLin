using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 吾有一友：选择一张额外牌组的卡给一个队友手牌，如果没有队友则获得吾即吾友能力，升级减一费。
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

        Creature target;
        if (teammates.Count > 0)
        {
            // 有队友：选择一个队友
            target = teammates[0];
        }
        else
        {
            // 没队友：选自己
            target = source.Owner.Creature;
        }

        // 选择一张额外牌组的卡
        var caseFilePile = MainFile.CaseFilePile.GetPile(source.Owner);
        if (caseFilePile.Cards.Count == 0) return;

        var cardPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var cardSelection = await CardSelectCmd.FromSimpleGrid(
            choiceContext, caseFilePile.Cards.ToList(), source.Owner, cardPrefs);
        var selectedCard = cardSelection.FirstOrDefault();
        if (selectedCard == null) return;

        // 复制给目标手牌，移除原卡
        var newCard = source.CombatState.CreateCard(selectedCard.CanonicalInstance, target.Player);
        if (source.IsUpgraded)
            CardCmd.Upgrade(newCard);
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, target.Player);
        await CardPileCmd.RemoveFromCombat(selectedCard);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
