using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSeventySix : ManosabaCardTemplate
{
    public CardSeventySix() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
        }
    }

    // 固定基础值
    protected override IEnumerable<DynamicVar> CanonicalVars => [
    
        new PowerVar<WithPower>(25m),
        new PowerVar<SuspectPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获得魔女化
        await PowerCmd.Apply<WithPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["WithPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 获得嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["WithPower"].UpgradeValueBy(5m);
      
    }
}
