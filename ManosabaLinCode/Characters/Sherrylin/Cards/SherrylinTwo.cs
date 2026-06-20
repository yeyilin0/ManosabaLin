using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public class SherrylinTwo() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SuspectPower>(1m),
        new CardsVar(3)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<SuspectPower>(choiceContext, Owner.Creature, DynamicVars["SuspectPower"].BaseValue, Owner.Creature, this, false);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
