using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Actions;
using ManosabaLin.Characters.Emalin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public class EmaEnding() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new EnergyVar(1), new CardsVar(1)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<EmaTrueEnding>();
            yield return HoverTipFactory.FromCard<EmaBadEnding>();
            yield return HoverTipFactory.FromPower<EmaBadEndingPower>();
            yield return HoverTipFactory.FromPower<EmaBadEndingRewardPower>();
            yield return HoverTipFactory.FromPower<EmaTrueEndingPower>();
        }
    }

    protected override IEnumerable<ICardComponent> CanonicalComponents => [new UniqueComponent()];

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond != null && (bond.Affinity >= 13 || bond.Estrangement >= 13))
            return new CardLocation(Owner, PileType.Hand, CardPilePosition.Bottom);

        return base.GetResultLocationForCardPlayC();
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond != null)
            bond.Affinity++;
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
            else if (bond.Estrangement >= 13 && bond.Estrangement >= bond.Affinity)
                await CardCmd.TransformTo<EmaBadEnding>(this);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
