using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Mllm() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(15m, ValueProp.Unpowered),
        new PowerVar<MllmPower>(1m),
        new PowerVar<SuspectPower>(1m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MllmPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 不受力量加成的伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .Execute(choiceContext);

        // 给予 MllmPower
        await PowerCmd.Apply<MllmPower>(
            choiceContext, target,
            source.DynamicVars["MllmPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 获得嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.BaseValue = 30m;
        DynamicVars["MllmPower"].BaseValue = 2m;
    }
}
