using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class AuroraCrimsonTwinbirthPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnSwordGenerationSucceeded(
        PlayerChoiceContext choiceContext,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        if (Amount <= 0)
            return;
        if (request.OriginalKind != SwordTokenKind.Aurora)
            return;
        if (!ReferenceEquals(request.Owner.Creature, Owner))
            return;

        var count = result.SuccessCount * (int)Amount;
        if (count <= 0)
            return;

        Flash();
        await SwordTokenGeneration.AddCrimsonSwordsToHand(choiceContext, request.Owner, count, this);
    }
}
