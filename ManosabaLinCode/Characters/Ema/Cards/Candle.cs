using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Emalin;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>蜡烛 - 1费技能, 赞同附魔, 4格挡, 一名同伴回3血, 全体1能量, 升级全体2能量</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class Candle : ManosabaEmalinCardTemplate
{
    public Candle() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.AgreeKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new BlockVar(4m, ValueProp.Move),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, 4m, ValueProp.Move, cardPlay);

        var allies = CombatState.Allies.Where(a => a is { IsAlive: true } && a != Owner.Creature).ToList();
        if (allies.Count > 0)
        {
            var target = Owner.RunState.Rng.CombatTargets.NextItem(allies);
            await CreatureCmd.Heal(target, 3m);
        }

        foreach (var player in CombatState.Players)
            await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1);
    }
}