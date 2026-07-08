using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SuspectCovenant() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RitualCeremonyPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        if (target == null || target == Owner.Creature || target.Side != Owner.Creature.Side) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var targetSuspect = target.GetPower<SuspectPower>();
        if (targetSuspect == null || targetSuspect.Amount <= 0) return;

        // 消耗目标的全部嫌疑
        var consumedAmount = targetSuspect.Amount;
        await PowerCmd.Remove(targetSuspect);

        // 自己获得等量嫌疑
        await PowerCmd.Apply<SuspectPower>(choiceContext, Owner.Creature, consumedAmount, Owner.Creature, this, false);

        // 检查自己的嫌疑是否恰好为12
        var mySuspect = Owner.Creature.GetPower<SuspectPower>();
        if (mySuspect?.Amount == 11)
        {
            await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this, false);
            await PowerCmd.Apply<RitualCeremonyPower>(choiceContext, target, 1m, Owner.Creature, this, false);

            // 升级效果：平分你与目标的嫌疑
            if (IsUpgraded)
            {
                var targetSuspectAfter = target.GetPower<SuspectPower>();
                var mySuspectAfter = Owner.Creature.GetPower<SuspectPower>();

                var targetAmount = targetSuspectAfter?.Amount ?? 0;
                var myAmount = mySuspectAfter?.Amount ?? 0;

                var total = targetAmount + myAmount;
                var half = total / 2;

                // 移除双方现有嫌疑
                if (targetSuspectAfter != null)
                    await PowerCmd.Remove(targetSuspectAfter);
                if (mySuspectAfter != null)
                    await PowerCmd.Remove(mySuspectAfter);

                // 平分给双方
                if (half > 0)
                {
                    await PowerCmd.Apply<SuspectPower>(choiceContext, Owner.Creature, half, Owner.Creature, this, false);
                    await PowerCmd.Apply<SuspectPower>(choiceContext, target, total - half, Owner.Creature, this, false);
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
