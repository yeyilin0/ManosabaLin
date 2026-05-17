using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class InvestigationPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var drawPile = PileType.Draw.GetPile(player).Cards;
        if (drawPile.Count == 0) return;

        var topCard = drawPile.Last();
        var prefs = new CardSelectorPrefs(
            new LocString("card_selection", "TO_DISCARD"), 0, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext, new[] { topCard }, player, prefs);

        // 选了才弃掉，没选就不变（卡牌留在抽牌堆顶）
        if (selected != null && selected.Any())
        {
            await CardPileCmd.Add(topCard, PileType.Discard);
        }
    }
}