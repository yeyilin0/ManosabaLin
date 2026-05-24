using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public class EmaEnding(): ManosabaCardTemplate(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new CardsVar(1)];

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new UniqueComponent()];

    protected override PileType GetResultPileTypeForCardPlayC()
    {
        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond != null && (bond.Affinity >= 13 || bond.Estrangement >= 13)) return PileType.Hand;
        return base.GetResultPileTypeForCardPlayC();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? source,
        ComponentContext componentContext)
    {
        if (card == this && oldPileType == PileType.Play && card.Pile?.Type == PileType.Hand)
        {
            var bond = Owner.Creature.GetPower<BondPower>();
            if (bond is null) return;
            if (bond.Affinity >= 13 && bond.Affinity >= bond.Estrangement)
                await CardCmd.TransformTo<EmaTrueEnding>(this);
            else if(bond.Estrangement >= 13 && bond.Estrangement >= bond.Affinity)
                await CardCmd.TransformTo<EmaBadEnding>(this);
        }
    }
}
