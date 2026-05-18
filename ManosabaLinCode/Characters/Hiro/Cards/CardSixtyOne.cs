using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardSixtyOne() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override bool GainsBlock => true;

    // 固定基础格挡 5
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new BlockVar(5m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级：格挡 +3（5 → 8）
        DynamicVars.Block.BaseValue += 3;
    }
}
