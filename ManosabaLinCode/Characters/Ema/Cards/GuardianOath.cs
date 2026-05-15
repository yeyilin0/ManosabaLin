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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class GuardianOath : ManosabaEmalinCardTemplate
{
    public GuardianOath() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        // 亲近+1
        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        // 对随机目标施加3层亚里沙的魔法
        var allTargets = CombatState.Allies.Concat(CombatState.Enemies)
            .Where(c => c is { IsAlive: true })
            .ToList();
        if (allTargets.Count > 0)
        {
            var rng = owner.RunState.Rng.CombatTargets;
            var target = rng.NextItem(allTargets);

            await PowerCmd.Apply<AmmPower>(
                choiceContext, target, 3, creature, this, false);

            // 如果给到队友
            if (target.Side == creature.Side && target != creature)
            {
                // 队友获得一点能量
                if (target.Player != null)
                    await PlayerCmd.GainEnergy(1m, target.Player);

                // 亲近大于疏远时再回一血
                if (bond != null && bond.Affinity > bond.Estrangement)
                    await CreatureCmd.Heal(target, 1m);
            }
            // 如果给到敌人
            else if (target.Side != creature.Side)
            {
                await PowerCmd.Apply<VulnerablePower>(
                    choiceContext, target, 1, creature, this, false);
            }
        }
    }
}
