using ManosabaLin.Characters.Heidemarie.Mechanics;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class SwordCurtainPower : ManosabaPowerTemplate, ISwordGenerationListener
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
        if (!ReferenceEquals(request.Owner.Creature, Owner))
            return;

        var auroraCount = result.SuccessCountFor(SwordTokenKind.Aurora);
        if (auroraCount <= 0)
            return;

        Flash();
        for (var i = 0; i < auroraCount; i++)
            await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null, fast: auroraCount > 1);
    }
}
