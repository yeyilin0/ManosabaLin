using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin;

/// <summary>
/// 案卷牌基类：打出时获得能力并挂载球体。
/// 球体消散或被顶掉时，能力一起移除。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public abstract class CaseFileCard<TOrb, TPower>(
    int energyCost, CardRarity rarity, TargetType targetType)
    : ManosabaCardTemplate(energyCost, CardType.Power, rarity, targetType, false)
    where TOrb : ModOrbTemplate
    where TPower : PowerModel
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new SherrylinOrbInitializerComponent()];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        // 获得能力
        var power = await PowerCmd.Apply<TPower>(
            choiceContext, Owner.Creature, 1,
            Owner.Creature, null, false);

        // 挂载球体，绑定能力
        var orb = (TOrb)ModelDb.Orb<TOrb>().MutableClone();
        if (orb is EmotionOrb emotionOrb)
            emotionOrb.BoundPower = power;
        await OrbCmd.Channel(choiceContext, orb, Owner);
    }
}
