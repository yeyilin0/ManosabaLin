using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
[RegisterCharacterStarterCard(typeof(Ananlin))]
[RegisterArchaicToothTranscendence(typeof(AnanlinLover))]
public sealed class AnanlinMentalTrauma() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(20m),
        new CardsVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WithPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<WithPower>(
            choiceContext, Owner.Creature,
            DynamicVars["WithPower"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        if (IsUpgraded)
            await CardPileCmd.Draw(choiceContext, DynamicVars["Cards"].BaseValue, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
