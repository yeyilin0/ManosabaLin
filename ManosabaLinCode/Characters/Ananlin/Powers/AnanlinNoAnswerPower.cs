using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinNoAnswerPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is not { } sketchbook) return;
        if (sketchbook.AttacksPlayedThisTurn > 0) return;

        Flash();
        await sketchbook.AddSilence(choiceContext, (int)Amount, null);
    }
}
