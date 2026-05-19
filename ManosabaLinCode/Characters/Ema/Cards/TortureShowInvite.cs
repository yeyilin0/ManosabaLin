using MinionLib.Component.Core;
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
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>拷问秀的邀请文 - 1费技能, 赞同附魔, 全体同伴4格挡, ≥2额外4格挡</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class TortureShowInvite : ManosabaCardTemplate
{
    public TortureShowInvite() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.AgreeKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
            await CreatureCmd.GainBlock(ally, 4m, ValueProp.Move, cardPlay);

        var agreementCount = EmalinCombatHelper.GetAgreementPlaysThisTurn(Owner.Creature, CombatState);
        if (agreementCount >= 2)
        {
            foreach (var ally in CombatState.Allies.Where(a => a is { IsAlive: true }))
                await CreatureCmd.GainBlock(ally, 4m, ValueProp.Move, cardPlay);
        }
    }
    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
