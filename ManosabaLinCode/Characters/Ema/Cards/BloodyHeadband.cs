using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>沾血的发带 - 1费技能, 赞同附魔, 5格挡+3同伴格挡, 升级各+3</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class BloodyHeadband : ManosabaEmalinCardTemplate
{
    public BloodyHeadband() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.AgreeKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new BlockVar("AllyBlock", 3m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        var allies = CombatState.Allies.Where(a => a is { IsAlive: true } && a != Owner.Creature).ToList();
        if (allies.Count > 0)
        {
            var target = Owner.RunState.Rng.CombatTargets.NextItem(allies);
            await CreatureCmd.GainBlock(target, DynamicVars["AllyBlock"].BaseValue, ValueProp.Move, cardPlay);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        DynamicVars["AllyBlock"].UpgradeValueBy(3m);
    }
}