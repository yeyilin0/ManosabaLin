using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 守绪之盾：获得护盾，下回合开始之前每受到一次攻击增加一层情绪，升级加护盾
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class EmotionShield() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionShieldPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.GainBlock(source.Owner.Creature, source.DynamicVars.Block, cardPlay);

        await PowerCmd.Apply<EmotionShieldPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}
