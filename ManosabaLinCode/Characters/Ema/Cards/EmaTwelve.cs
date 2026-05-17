using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaTwelve : ManosabaEmalinCardTemplate
{
    public EmaTwelve() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer) { }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ConsumeAmount", 2m),
        new PowerVar<SuspectPower>(3m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<YlsmPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 消耗目标的嫌疑
        var targetSuspectPower = target.GetPower<SuspectPower>();
        if (targetSuspectPower != null && targetSuspectPower.Amount > 0)
        {
            var consumeAmount = System.Math.Min(targetSuspectPower.Amount, source.DynamicVars["ConsumeAmount"].IntValue);
            await PowerCmd.ModifyAmount(
                choiceContext, targetSuspectPower,
                -consumeAmount,
                target,
                source,
                false
            );
        }

        // 给自己施加嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 给目标能量
        await PlayerCmd.GainEnergy(
            source.DynamicVars.Energy.IntValue,
            target.Player
        );

        // 给自己 1 层 YlsmPower
        await PowerCmd.Apply<YlsmPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["ConsumeAmount"].UpgradeValueBy(1m);
        DynamicVars["SuspectPower"].UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}
