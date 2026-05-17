using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSeventyTwo : ManosabaCardTemplate
{
    public CardSeventyTwo() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 第一步：手牌里所有带有轮回关键词的卡牌本回合免费
        var handPile = PileType.Hand.GetPile(source.Owner);
        foreach (var card in handPile.Cards)
            if (card.HasModKeyword(TransmigrationRules.TransmigrationKeywordId) && !card.EnergyCost.CostsX)
                card.SetToFreeThisTurn();

        // 第二步：获得不能再抽卡的能力
        await PowerCmd.Apply<NoDrawPower>(
            choiceContext, source.Owner.Creature,
            1m,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1); // 能耗 3 → 2
    }
}