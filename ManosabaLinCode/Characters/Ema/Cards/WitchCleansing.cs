using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class WitchCleansing : ManosabaEmalinCardTemplate
{
    public WitchCleansing() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Unpowered)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        // 疏远+1
        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        // 对自己造成5点伤害（不受力量加成）
        await CreatureCmd.Damage(choiceContext, creature,
            DynamicVars.Damage.BaseValue,
            ValueProp.Unpowered | ValueProp.Move, creature, this);

        // 移除全体友方四分之一的魔女化层数
        var totalRemoved = 0m;
        foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
        {
            var withPower = ally.GetPower<WithPower>();
            if (withPower == null || withPower.Amount <= 0) continue;

            var removeAmount = (int)(withPower.Amount / 4m);
            if (removeAmount <= 0) continue;

            totalRemoved += removeAmount;
            await PowerCmd.ModifyAmount(
                choiceContext, withPower, -removeAmount, creature, this, false);
        }

        // 疏远大于亲近时，移除的魔女化转移到自己身上
        if (bond != null && bond.Estrangement > bond.Affinity && totalRemoved > 0)
        {
            await PowerCmd.Apply<WithPower>(
                choiceContext, creature, totalRemoved, creature, this, false);
        }
    }
}
