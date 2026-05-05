using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class RitualCeremonyPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.ForEnergy(this);
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        var source = this;
        if (side != source.Owner.Side) return;
        source.Flash();

        var player = source.Owner.Player;
        var netId = LocalContext.NetId;
        if (!netId.HasValue) return;

        // 创建一个 HookPlayerChoiceContext 用于 Draw 调用
        var choiceContext = new HookPlayerChoiceContext(player, netId.Value, GameActionType.Combat);

        await PlayerCmd.GainEnergy(source.Amount, player);
        await CardPileCmd.Draw(choiceContext, source.Amount, player);
        await PowerCmd.Apply<StrengthPower>(choiceContext, source.Owner, source.Amount, source.Owner, null);
    }
}