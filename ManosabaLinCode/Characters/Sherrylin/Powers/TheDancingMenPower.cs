using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class TheDancingMenPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var drawPile = PileType.Draw.GetPile(Owner.Player);
        if (drawPile.Cards.Count == 0) return;

        Flash();

        var prefs = new CardSelectorPrefs(new LocString("TheDancingMen", "选择一张牌消耗"), 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile.Cards.ToList(), Owner.Player, prefs);
        var selectedCard = selected.FirstOrDefault();
        if (selectedCard == null) return;

        var cardCost = selectedCard.EnergyCost.Canonical;

        await CardCmd.Exhaust(choiceContext, selectedCard);

        var bonus = cardCost + (int)Amount;
        if (bonus > 0)
        {
            await PowerCmd.Apply<TempStrength>(
                choiceContext,
                Owner,
                bonus,
                Owner,
                null,
                false);
        }

        await PlayerCmd.GainEnergy(1, Owner.Player);
    }
}
