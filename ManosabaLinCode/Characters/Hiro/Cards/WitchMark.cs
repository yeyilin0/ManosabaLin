using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class WitchMark() : ManosabaCardTemplate(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<WithPower>(30m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WithPower>(); }
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var source = this;

        await PowerCmd.Apply<WithPower>(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["WithPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await PowerCmd.Apply<WithPower>(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["WithPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WithPower"].UpgradeValueBy(4m);
    }
}
