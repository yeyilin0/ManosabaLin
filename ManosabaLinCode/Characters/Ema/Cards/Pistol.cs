using MinionLib.Component.Core;
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
using ManosabaLin.Characters.Common.Powers;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Pistol : ManosabaCardTemplate
{
    public Pistol() : base(2, CardType.Skill, CardRarity.Common, TargetType.AnyPlayer) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.AgreeCardKeyword };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TempStrength>(3m),
        new BlockVar(4m, ValueProp.Move)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var creature = Owner.Creature;
        var target = cardPlay.Target ?? creature;

        await PowerCmd.Apply<StrengthPower>(choiceContext, target, DynamicVars["TempStrength"].BaseValue, creature, this);
        await CreatureCmd.GainBlock(creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["TempStrength"].UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
