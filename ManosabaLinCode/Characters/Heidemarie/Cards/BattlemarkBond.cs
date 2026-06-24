using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class BattlemarkBond() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BattlemarkBondPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BattlemarkBondPower>();
            yield return HoverTipFactory.FromPower<MarkPower>();
        }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            if (IsUpgraded)
                yield return CardKeyword.Innate;
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<BattlemarkBondPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BattlemarkBondPower"].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}
