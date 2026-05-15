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
public sealed class ShatteredResonance : ManosabaEmalinCardTemplate
{
    public ShatteredResonance() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

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

        // 获得一层汉娜的魔法
        await PowerCmd.Apply<HnmPower>(
            choiceContext, creature, 1, creature, this, false);

        // 敌人全体获得5点力量
        foreach (var enemy in CombatState.Enemies.Where(e => e is { IsAlive: true }))
        {
            await PowerCmd.Apply<StrengthPower>(
                choiceContext, enemy, 5, creature, this, false);
        }

        // 疏远大于亲近时，敌人降低10力量
        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            foreach (var enemy in CombatState.Enemies.Where(e => e is { IsAlive: true }))
            {
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext, enemy, -10, creature, this, false);
            }
        }
    }
}
