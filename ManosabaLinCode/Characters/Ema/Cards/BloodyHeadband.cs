using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class BloodyHeadband : ManosabaCardTemplate
{
    public BloodyHeadband() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.AgreeKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new EnergyVar("EnergyGain", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var creature = Owner.Creature;
        var allies = CombatState.Allies.Where(a => a is { IsAlive: true } && a != creature).ToList();

        // 澶氫汉鏃堕€夐槦鍙嬶紝鍗曚汉榛樿鑷繁
        var target = allies.Count > 0
            ? Owner.RunState.Rng.CombatTargets.NextItem(allies)
            : creature;

        await CreatureCmd.GainBlock(creature, DynamicVars.Block, cardPlay);
        await PlayerCmd.GainEnergy(DynamicVars["EnergyGain"].BaseValue, target.Player ?? Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(3m);
        EnergyCost.UpgradeBy(-1);
    }
}
