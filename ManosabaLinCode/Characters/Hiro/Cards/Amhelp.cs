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
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Amhelp() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
        new DynamicVar("HealAmount", 3m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 自己失去 6 点生命
        await CreatureCmd.Damage(
            choiceContext,
            source.Owner.Creature,
            6m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null
        );

        // 全体队友获得护盾
        var allies = source.Owner.Creature.CombatState.Creatures
            .Where(c => c.IsAlive && !c.IsEnemy)
            .ToList();

        foreach (var ally in allies)
        {
            await CreatureCmd.GainBlock(ally, source.DynamicVars.Block, cardPlay);
            // 全体回复 3 点生命
            await CreatureCmd.Heal(ally, source.DynamicVars["HealAmount"].BaseValue);
            // 降低 30 魔女化
            var with = ally.GetPower<WithPower>();
            if (with != null && with.Amount > 0)
            {
                var reduceAmount = System.Math.Min(with.Amount, 30m);
                if (with.Amount == reduceAmount)
                    await PowerCmd.Remove(with);
                else
                    await PowerCmd.ModifyAmount(choiceContext, with, -reduceAmount, ally, source, false);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(5m);
        DynamicVars["HealAmount"].UpgradeValueBy(2m);
    }
}