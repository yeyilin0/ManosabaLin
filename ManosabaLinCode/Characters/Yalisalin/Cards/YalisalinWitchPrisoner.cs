using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class YalisalinWitchPrisoner() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TempStrength>("TempStrength", 6m),
        new PowerVar<TempDexterity>("TempDexterity", 6m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TempStrength>();
            yield return HoverTipFactory.FromPower<TempDexterity>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<TempStrength>(
            choiceContext, Owner.Creature,
            DynamicVars["TempStrength"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        await PowerCmd.Apply<TempDexterity>(
            choiceContext, Owner.Creature,
            DynamicVars["TempDexterity"].BaseValue,
            Owner.Creature,
            this,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
