using MinionLib.Component.Core;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
[RegisterCharacterStarterCard(typeof(Yalisalin))]
public sealed class YalisalinWitchTrial() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>(1m),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SilentPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<SilentPower>(
            choiceContext, Owner.Creature,
            DynamicVars["SilentPower"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
