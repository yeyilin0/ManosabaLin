using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 我一只手就可以把你扣晕：选择一个队友使其获得无法出牌，给予队友缓冲，自己获得能量，打出移除，没队友自动选自己，升级给队友两层缓冲。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class OneHandKnockout() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new RemoveOnPlayComponent()];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BufferPower>(1m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BufferPower>();
            yield return HoverTipFactory.FromPower<CannotPlayCardsPower>();
        }
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
            // 有队友：选择一个（简化为选第一个）
            target = teammates[0];
        }
        else
        {
            // 没队友：选自己
            target = source.Owner.Creature;
        }

        // 给予无法出牌
        await PowerCmd.Apply<CannotPlayCardsPower>(
            choiceContext, target, 1,
            source.Owner.Creature, source, false);

        // 给予缓冲
        await PowerCmd.Apply<BufferPower>(
            choiceContext, target,
            source.DynamicVars["BufferPower"].IntValue,
            source.Owner.Creature, source, false);

        // 自己获得能量
        await PlayerCmd.GainEnergy(
            source.DynamicVars.Energy.IntValue,
            source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BufferPower"].UpgradeValueBy(1m);
    }
}
