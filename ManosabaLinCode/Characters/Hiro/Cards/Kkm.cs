using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Kkm() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyPlayer)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(1m, ValueProp.Unpowered),
        new PowerVar<KkmPower>(1m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<KkmPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await CreatureCmd.Damage(choiceContext, target,
            source.DynamicVars.Damage.BaseValue,
            ValueProp.Unpowered | ValueProp.Move, source);

        await PowerCmd.Apply<KkmPower>(
            choiceContext, target,
            source.DynamicVars["KkmPower"].BaseValue,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["KkmPower"].UpgradeValueBy(1m);
    }
}