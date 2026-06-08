using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Orbs;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin;

/// <summary>
/// 案卷牌基类：从案卷牌堆打出时挂载到充能球槽位，最多同时生效两个。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public abstract class CaseFileCard(int energyCost, CardRarity rarity, TargetType targetType)
    : ManosabaCardTemplate(energyCost, CardType.Power, rarity, targetType, false)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new SherrylinOrbInitializerComponent()];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await OrbCmd.Channel(choiceContext, CreateEmotionOrb(), Owner);
    }

    protected abstract OrbModel CreateEmotionOrb();
}
