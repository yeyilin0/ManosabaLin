using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Sherrylin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class Sherrymonvqiufan() : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TempStrength>();
            yield return HoverTipFactory.FromPower<TempDexterity>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TempStrength>("TempStrength", 6m),
        new PowerVar<TempDexterity>("TempDexterity", 6m),
        new DamageVar(7, DamageProps.cardUnpowered)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<TempStrength>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["TempStrength"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        await PowerCmd.Apply<TempDexterity>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["TempDexterity"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(cardPlay.Target!)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}