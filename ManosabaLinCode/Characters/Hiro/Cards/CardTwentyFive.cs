using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwentyFive : ManosabaCardTemplate
{
    public CardTwentyFive() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<CardTwentySix>(IsUpgraded); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 1. 获取手牌并记录数量
        var handPile = PileType.Hand.GetPile(source.Owner);
        var handCards = handPile.Cards.ToList();
        int handSize = handCards.Count;

        if (handSize == 0)
            return;

        // 2. 丢弃所有手牌
        await CardCmd.Discard(choiceContext, handCards);
        await Cmd.CustomScaledWait(0.0f, 0.25f);

        // 3. 生成等量的 CardTwentySix
        for (var i = 0; i < handSize; i++)
        {
            // 修正：使用 CombatState.CreateCard
            var card = source.CombatState.CreateCard<CardTwentySix>(source.Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, source.Owner);

            // 4. 如果升级了，生成的牌也升级
            if (IsUpgraded) CardCmd.Upgrade(card);

            await Cmd.Wait(0.1f);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 效果已在 OnPlay 中通过 IsUpgraded 处理
    }
}