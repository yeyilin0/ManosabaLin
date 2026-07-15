using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 耗念引牌能力：回合开始选择一张手卡消耗，然后抽牌。
/// </summary>
[RegisterPower]
public sealed class MindCostDrawPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var hand = PileType.Hand.GetPile(Owner.Player).Cards.ToList();
        if (hand.Count == 0) return;

        Flash();

        var prefs = new CardSelectorPrefs(new LocString("MindCostDraw", "选择一张卡消耗"), 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, hand, Owner.Player, prefs);
        var selectedList = selected.ToList();
        if (selectedList.Count == 0) return;

        await CardPileCmd.RemoveFromCombat(selectedList[0]);

        await CardPileCmd.Draw(choiceContext, (int)Amount, Owner.Player);
    }
}
