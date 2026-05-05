using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class CardTwentyPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 监听伪证层数变化
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, // ★ 新增
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource)
    {
        var source = this;


        // 只关心伪证的变化
        if (power is not PerjuryPower)
            return;

        // 只关心增加的情况
        if (amountChanged <= 0)
            return;

        // 确保是持有者的能力
        if (power.Owner != source.Owner)
            return;

        // 获取增加前的层数和增加后的层数
        var oldAmount = power.Amount - (int)amountChanged;
        var newAmount = power.Amount;

        // 计算跨越了多少个 4 的倍数
        var oldTriggers = oldAmount / 4;
        var newTriggers = newAmount / 4;
        var triggerCount = newTriggers - oldTriggers;

        for (var i = 0; i < triggerCount; i++)
        {
            source.Flash();

            // 先回复能量
            await PlayerCmd.GainEnergy(1m, source.Owner.Player);

            // 再抽牌
            await CardPileCmd.Draw(choiceContext, 1m, source.Owner.Player);
        }
    }
}