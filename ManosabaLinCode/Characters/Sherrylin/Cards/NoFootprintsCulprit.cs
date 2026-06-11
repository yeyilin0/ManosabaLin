using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 没有脚印，犯人是…：
/// 清空你的减力量，选择一个队友获得等于你当前嫌疑一半的嫌疑，使其获得等量的力量，
/// 若获得3层则令队友和自己都获得一层汉娜的魔法。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class NoFootprintsCulprit() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<HnmPower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var combatState = source.CombatState;
        if (combatState == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 清空减力量
        var tempStrDown = source.Owner.Creature.GetPower<TempStrengthDown>();
        if (tempStrDown != null && tempStrDown.Amount > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, tempStrDown, -tempStrDown.Amount,
                source.Owner.Creature, source, false);
        }

        // 获取当前嫌疑层数
        var suspectPower = source.Owner.Creature.GetPower<SuspectPower>();
        var suspectAmount = suspectPower?.Amount ?? 0;
        var halfSuspect = suspectAmount / 2;

        // 选择一个队友
        var teammates = combatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c is { IsAlive: true, IsPlayer: true })
            .ToList();

        Creature target = teammates.Count > 0 ? teammates[0] : source.Owner.Creature;

        // 使队友获得嫌疑和力量
        if (halfSuspect > 0)
        {
            await PowerCmd.Apply<SuspectPower>(
                choiceContext, target, halfSuspect,
                source.Owner.Creature, source, false);

            await PowerCmd.Apply<TempStrength>(
                choiceContext, target, halfSuspect,
                source.Owner.Creature, source, false);

            // 若获得3层则令队友和自己都获得一层汉娜的魔法
            if (halfSuspect >= 3)
            {
                await PowerCmd.Apply<HnmPower>(
                    choiceContext, target, 1,
                    source.Owner.Creature, source, false);
                await PowerCmd.Apply<HnmPower>(
                    choiceContext, source.Owner.Creature, 1,
                    source.Owner.Creature, source, false);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
