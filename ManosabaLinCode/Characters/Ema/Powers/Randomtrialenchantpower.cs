using Godot;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Randomtrialenchantpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        var drawPile = PileType.Draw.GetPile(Owner.Player);
        var drawCards = drawPile.Cards.Where(c => c.Enchantment == null && c.Rarity != CardRarity.Token && c.Type != CardType.Status && c.Type != CardType.Curse).ToList();
        if (drawCards.Count == 0) return;

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var targetCard = rng.NextItem(drawCards);

        var enchantTypes = new[] { typeof(Rebuttal), typeof(Agreement), typeof(Doubt) };
        var chosenEnchant = rng.NextItem(enchantTypes);

        var rebuttalCanonical = ModelDb.Enchantment<Rebuttal>();
        var agreementCanonical = ModelDb.Enchantment<Agreement>();
        var doubtCanonical = ModelDb.Enchantment<Doubt>();

        if (chosenEnchant == typeof(Rebuttal))
            CardCmd.Enchant(rebuttalCanonical.ToMutable(), targetCard, 1m);
        else if (chosenEnchant == typeof(Agreement))
            CardCmd.Enchant(agreementCanonical.ToMutable(), targetCard, 1m);
        else
            CardCmd.Enchant(doubtCanonical.ToMutable(), targetCard, 1m);

        Flash();
    }
}