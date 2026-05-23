using ManosabaLin.Characters.Emalin.Actions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Core;
using MinionLib.Component.Utils;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class EmaTrueEndingRewardComponent : AmountCardComponent
{
    public override IEnumerable<IHoverTip> HoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<EmaTrueEndingRewardAction>();

    public EmaTrueEndingRewardComponent(int amount = 1)
    {
        Amount = amount;
    }

    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await PowerCmd.Apply<EmaTrueEndingRewardAction>(
            choiceContext,
            Card!.Owner.Creature,
            Amount,
            Card!.Owner.Creature,
            Card
        );
    }
}
