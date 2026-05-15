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
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SharedFate : ManosabaEmalinCardTemplate
{
    public SharedFate() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

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

        // 获得五层雪莉的魔法
        await PowerCmd.Apply<XlmPower>(
            choiceContext, creature, 5, creature, this, false);

        // 亲近大于疏远时，队友增加五点力量
        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
            {
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext, ally, 5, creature, this, false);
            }
        }
    }
}
