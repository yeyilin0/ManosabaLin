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
public sealed class CardFive : ManosabaCardTemplate
{
    public CardFive() : base(0, CardType.Attack, CardRarity.Common, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<JusticePower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(13m, ValueProp.Move),
        new PowerVar<JusticePower>(1m) // 仅用于本地化显示
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 1. 获取当前正义层数
        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        var justiceAmount = justicePower?.Amount ?? 0;

        if (justiceAmount > 0)
        {
            // 2. 消耗所有正义层数
            await PowerCmd.ModifyAmount(
                choiceContext, // ★ 第一个参数
                justicePower!,
                -justiceAmount,
                source.Owner.Creature,
                source,
                false
            );

            // 3. 根据消耗的正义层数造成对应次数的伤害
            await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
                .WithHitCount(justiceAmount)
                .FromCard(source)
                .TargetingRandomOpponents(source.CombatState)
                .WithHitFx("vfx/vfx_starry_impact")
                .Execute(choiceContext);
        }
        // 如果没有正义层数，不造成伤害（攻击次数为 0）
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 升级后伤害 13 → 15
    }
}