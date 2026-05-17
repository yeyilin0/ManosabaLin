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
public sealed class Ten : ManosabaCardTemplate
{
    public Ten() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<SuspectPower>(1m) // 仅用于本地化显示
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 1. 获取当前嫌疑层数
        var suspectPower = source.Owner.Creature.GetPower<SuspectPower>();
        var suspectAmount = suspectPower?.Amount ?? 0;

        if (suspectAmount > 0)
            // 2. 根据嫌疑层数造成对应次数的伤害（不消耗嫌疑）
            await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
                .WithHitCount(suspectAmount)
                .FromCard(source)
                .TargetingRandomOpponents(source.CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

        // 3. 获得 1 层嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            1m,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1); // 能耗 2 → 1
    }
}