using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Lyqinjin : ManosabaEmalinCardTemplate
{
    public Lyqinjin() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyAlly) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<BondPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var combatState = source.CombatState;

        // 亲近 +1
        var bond = creature.GetPower<BondPower>();
        if (bond != null)
            bond.Affinity++;

        // 确定目标：多人时选队友，单人默认自己
        Creature targetAlly;
        if (cardPlay.Target != null)
            targetAlly = cardPlay.Target;
        else
            targetAlly = creature;

        // 亲近 > 疏远时，帮队友减少的嫌疑为2层
        var selfReduce = 1;
        var allyReduce = 1;
        if (bond != null && bond.Affinity > bond.Estrangement)
            allyReduce = 2;

        // 减少嫌疑
        var doubtOwner = creature.GetPower<SuspectPower>();
        var doubtTarget = targetAlly.GetPower<SuspectPower>();
        var totalLost = 0;

        if (doubtOwner != null && doubtOwner.Amount > 0)
        {
            var reduce = Math.Min(doubtOwner.Amount, selfReduce);
            doubtOwner.Amount -= reduce;
            totalLost += reduce;
        }
        if (doubtTarget != null && doubtTarget.Amount > 0)
        {
            var reduce = Math.Min(doubtTarget.Amount, allyReduce);
            doubtTarget.Amount -= reduce;
            totalLost += reduce;
        }

        if (totalLost <= 0) return;

        // 将减少的层数随机分配给任意敌方
        var enemies = combatState.GetOpponentsOf(creature)
            .Where(e => e.IsAlive)
            .ToList();

        if (enemies.Count == 0) return;

        var rng = owner.RunState.Rng.CombatCardSelection;
        for (int i = 0; i < totalLost; i++)
        {
            var target = rng.NextItem(enemies);
            await PowerCmd.Apply<SuspectPower>(
                choiceContext, target, 1m,
                creature, source, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}