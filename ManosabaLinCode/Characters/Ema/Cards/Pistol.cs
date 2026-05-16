using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Emalin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>手枪 - 2费技能, 赞同附魔, 一名同伴3力量, 4格挡, 升级各+2</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class Pistol : ManosabaEmalinCardTemplate
{
    public Pistol() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.AgreeKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(3m),
        new BlockVar(4m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var allies = CombatState.Allies.Where(a => a is { IsAlive: true } && a != Owner.Creature).ToList();
        if (allies.Count > 0)
        {
            var target = Owner.RunState.Rng.CombatTargets.NextItem(allies);
            await PowerCmd.Apply<StrengthPower>(choiceContext, target, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
        }

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthPower"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}