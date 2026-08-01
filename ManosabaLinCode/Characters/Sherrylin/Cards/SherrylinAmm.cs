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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SherrylinAmm() : ManosabaCardTemplate(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => AmmCardMath.CreateVars();

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<AmmPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await DamageCmd.Attack(source.DynamicVars.CalculatedDamage)
            .FromCard(source, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await PowerCmd.Apply<AmmPower>(
            choiceContext, source.Owner.Creature, source.DynamicVars["AmmPower"].BaseValue,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature, source, false);

        await PowerCmd.Apply<WithPower>(
            choiceContext, source.Owner.Creature, source.DynamicVars["WithPower"].BaseValue,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.CalculationBase.UpgradeValueBy(4m);
    }
}
