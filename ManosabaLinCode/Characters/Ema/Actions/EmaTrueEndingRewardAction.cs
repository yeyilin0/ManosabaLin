using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.Characters.Emalin.Actions;

public sealed class EmaTrueEndingRewardAction : ManosabaActionTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override TargetType TargetType => TargetType.AnyEnemy;

    public override bool DecrementAfterAct => true;

    protected override async Task OnAct(PlayerChoiceContext choiceContext, Creature? target)
    {
        ArgumentNullException.ThrowIfNull(target);
        await CreatureCmd.Damage(choiceContext, target, (decimal)target.CurrentHp / 4, ValueProp.Unblockable, Owner);
    }
}
