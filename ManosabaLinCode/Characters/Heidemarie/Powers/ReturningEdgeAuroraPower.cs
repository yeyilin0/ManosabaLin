using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Heidemarie.Mechanics;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class ReturningEdgeAuroraPower : ManosabaPowerTemplate, IBounceReturnListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnBounceReturnedToHand(
        PlayerChoiceContext choiceContext,
        BounceReturnEvent bounceReturn)
    {
        if (Amount <= 0)
            return;
        if (!ReferenceEquals(bounceReturn.Owner.Creature, Owner))
            return;

        var count = (int)Amount;
        if (count <= 0)
            return;

        Flash();
        await SwordTokenGeneration.AddAuroraSwordsToHand(choiceContext, bounceReturn.Owner, count, this);
    }
}
