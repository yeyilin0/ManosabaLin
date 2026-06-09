using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
[RegisterCharacterStarterCard(typeof(Sherrylin))]
public class SherrylinMentalTrauma : ManosabaCardTemplate
{
    public SherrylinMentalTrauma() : base(1, CardType.Power, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PowerCmd.Apply<EmotionPower>(
            choiceContext, Owner.Creature,
            1,
            Owner.Creature,
            this,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        AddKeyword(CardKeyword.Innate);
    }
}
