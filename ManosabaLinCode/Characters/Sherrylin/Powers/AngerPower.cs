using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class AngerPower : ManosabaPowerTemplate
{
    private const int Threshold = 10;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Amount < Threshold) return;

        Flash();

        // 达到10层：将1张愤怒加入案卷牌堆
        var combatState = Owner.CombatState;
        if (combatState != null)
        {
            var card = combatState.CreateCard<Anger>(Owner.Player);
            await CardPileCmd.Add(card, MainFile.CaseFilePile, CardPilePosition.Top);
        }

        // 重置层数
        await PowerCmd.Remove(this);
    }
}
