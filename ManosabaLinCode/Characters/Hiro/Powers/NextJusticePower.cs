using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class JusticeNextTurnPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;

        if (player != source.Owner.Player)
            return;

        source.Flash();

        // 获得等于层数的正义
        await PowerCmd.Apply<JusticePower>(
            choiceContext, source.Owner,
            source.Amount,
            source.Owner,
            null
        );

        // 一次性能力，触发后移除
        await PowerCmd.Remove(source);
    }
}