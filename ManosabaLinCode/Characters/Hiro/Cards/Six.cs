using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Six() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SuspectPower>(10),
        new PowerVar<IntangiblePower>(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<IntangiblePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["SuspectPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        await PowerCmd.Apply<IntangiblePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["IntangiblePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["IntangiblePower"].UpgradeValueBy(1m);
    }
}