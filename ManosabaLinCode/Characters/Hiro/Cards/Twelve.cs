using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Twelve() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    private const int RequiredSuspectAmount = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ConsumeAmount", 2m),
        new PowerVar<SuspectPower>(3m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 消耗目标的嫌疑
        var targetSuspectPower = target.GetPower<SuspectPower>();
        if (targetSuspectPower != null && targetSuspectPower.Amount > 0)
        {
            var consumeAmount = Math.Min(targetSuspectPower.Amount, source.DynamicVars["ConsumeAmount"].IntValue);
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
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["ConsumeAmount"].UpgradeValueBy(1m);
        DynamicVars["SuspectPower"].UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}