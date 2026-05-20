using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ManosabaLin.Characters.Ema.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class BondExchangepower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private static readonly Type[] EstrangementTypes =
    [
        typeof(BalloonFragments), typeof(StabbingBlade), typeof(ShatteredResonance),
        typeof(WitchCleansing), typeof(ChainedTrust), typeof(PawnRealization),
        typeof(NoahEstrangement), typeof(MargaretEstrangement),
        typeof(CocoEstrangement), typeof(AnnEstrangement)
    ];

    private static readonly Type[] AffinityTypes =
    [
        typeof(SwapBodySuccess), typeof(GuardianOath), typeof(SharedFate),
        typeof(DollGift), typeof(TheOnlyClue), typeof(SubstituteCost),
        typeof(NoahAffinity), typeof(MargaretAffinity),
        typeof(CocoAffinity), typeof(AnnAffinity)
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Owner.IsDead) return;

        var bond = Owner.GetPower<BondPower>();
        if (bond == null) return;

        var owner = Owner.Player;
        var rng = owner.RunState.Rng.CombatCardSelection;
        var combatState = Owner.CombatState;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);

        var canEstrange = bond.Estrangement >= 2;
        var canAffinity = bond.Affinity >= 2;

        if (!canEstrange && !canAffinity) return;

        bool useEstrangement;
        if (canEstrange && canAffinity)
            useEstrangement = rng.NextDouble() < 0.5;
        else
            useEstrangement = canEstrange;

        if (useEstrangement)
        {
            bond.Estrangement -= 2;

            var handCards = PileType.Hand.GetPile(owner).Cards;
            if (handCards.Count > 0)
            {
                var prefs = new CardSelectorPrefs(new LocString("powers", "BondExchangepower.select_estrangement"), 1, 1);
                var selected = await CardSelectCmd.FromHand(choiceContext, owner, prefs, null, null);
                var original = selected.FirstOrDefault();

                if (original != null)
                {
                    await CardCmd.Exhaust(choiceContext, original);

                    var chosenType = rng.NextItem(EstrangementTypes);
                    var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
                    var newCard = (CardModel)genericMethod.Invoke(combatState, [owner]);
                    newCard.SetToFreeThisTurn();
                    await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner, CardPilePosition.Bottom);
                }
            }
        }
        else
        {
            bond.Affinity -= 2;

            var chosenType = rng.NextItem(AffinityTypes);
            var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
            var newCard = (CardModel)genericMethod.Invoke(combatState, [owner]);
            newCard.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner, CardPilePosition.Bottom);
        }

        Flash();
    }
}