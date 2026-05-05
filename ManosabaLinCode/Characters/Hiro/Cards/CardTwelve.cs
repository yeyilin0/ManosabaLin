using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwelve() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded) yield return CardKeyword.Innate;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TempStrength>();
            yield return HoverTipFactory.FromPower<TempDexterity>();
            yield return HoverTipFactory.FromPower<PerjuryPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TempStrength>("TempStrength", 6m),
        new PowerVar<TempDexterity>("TempDexterity", 5m),
        new PowerVar<PerjuryPower>("Perjury", 9m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 第一步：获得 6 层临时力量
        await PowerCmd.Apply<TempStrength>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["TempStrength"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 第二步：获得 5 层临时敏捷
        await PowerCmd.Apply<TempDexterity>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["TempDexterity"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 第三步：获得 9 层伪证
        await PowerCmd.Apply<PerjuryPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["Perjury"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}