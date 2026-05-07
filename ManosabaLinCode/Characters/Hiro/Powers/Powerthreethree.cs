using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class Powerthreethree : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;
        if (player != source.Owner.Player) return;

        var with = source.Owner.GetPower<WithPower>();
        var withAmount = with?.Amount ?? 0;
        var cardCount = (int)(withAmount / 100);
        if (cardCount <= 0) return;

        var drawPile = PileType.Draw.GetPile(player);
        var attackCards = drawPile.Cards
            .Where(c => c.Type == CardType.Attack && !c.Keywords.Contains(CardKeyword.Unplayable))
            .ToList();

        if (attackCards.Count == 0) return;

        // 洗乱
        var rng = new Random();
        for (var i = attackCards.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (attackCards[i], attackCards[j]) = (attackCards[j], attackCards[i]);
        }

        foreach (var card in attackCards.Take(cardCount))
        {
            card.SetToFreeThisTurn();
            await CardPileCmd.Add(card, PileType.Hand);
        }

        source.Flash();
    }
}