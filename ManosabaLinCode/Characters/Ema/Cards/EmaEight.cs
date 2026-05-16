using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaEight() : ManosabaEmalinCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const int RequiredSuspectAmount = 1;

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new PowerVar<SuspectPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            var suspectPower = Owner.Creature.GetPower<SuspectPower>();
            var suspectAmount = suspectPower?.Amount ?? 0;

            return suspectAmount >= RequiredSuspectAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 消耗 1 层嫌疑
        var suspectPower = source.Owner.Creature.GetPower<SuspectPower>();
        if (suspectPower != null && suspectPower.Amount >= RequiredSuspectAmount)
            await PowerCmd.ModifyAmount(
                choiceContext, suspectPower,
                -RequiredSuspectAmount,
                source.Owner.Creature,
                source,
                false
            );

        // 获得格挡
        await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars.Block.BaseValue += 3;
    }
}
