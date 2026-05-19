using Godot;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using System;
using System.Reflection;
using ManosabaLin.Characters.Ema.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Hiroshuyuanpower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        Flash();

        var bond = Owner.GetPower<BondPower>();

        // 疏远 +1
        if (bond != null)
            bond.Estrangement++;

        // 选择一张手牌变成随机疏远牌
        var handPile = PileType.Hand.GetPile(Owner.Player);
        var handCards = handPile.Cards.ToList();
        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(new LocString("ui", "select_card_transform"), 1, 1);
        var selected = await CardSelectCmd.FromHand(
            choiceContext, Owner.Player, prefs, null, null);
        var picked = selected.FirstOrDefault();
        if (picked == null) return;

        var estrangementCards = new[]
        {
            typeof(BalloonFragments),
            typeof(StabbingBlade),
            typeof(ShatteredResonance),
            typeof(WitchCleansing),
            typeof(ChainedTrust),
            typeof(PawnRealization),
            typeof(NoahEstrangement),
            typeof(MargaretEstrangement),
            typeof(CocoEstrangement),
            typeof(AnnEstrangement),
        };

        var combatState = Owner.Player.Creature.CombatState;
        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var chosenType = rng.NextItem(estrangementCards);

        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });
        var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
        var newCard = (CardModel)genericMethod.Invoke(combatState, new object[] { Owner.Player });

        await CardCmd.Transform(picked, newCard);
    }
}