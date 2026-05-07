using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Attackfivefive() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.Static(StaticHoverTip.Fatal);
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        var attack = await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!shouldTriggerFatal || !attack.Results.Any(r => r.WasTargetKilled))
            return;

        var justice = source.Owner.Creature.GetPower<JusticePower>();
        var healAmount = justice?.Amount ?? 0;
        if (healAmount > 0)
            await CreatureCmd.Heal(source.Owner.Creature, healAmount);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}