using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Entities.Powers;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class SilentPower : ManosabaPowerTemplate, IEasyRightClickablePower
{
    private const int SilenceCost = 13;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool CanHandleRightClickLocal(RightClickContext context)
    {
        return context.Model == this
            && context.Player == Owner.Player
            && Amount >= SilenceCost
            && Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault()?.CanTriggerSilenceRewrite() == true;
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (Amount < SilenceCost) return;
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is not { } sketchbook) return;
        if (!sketchbook.CanTriggerSilenceRewrite()) return;

        await PowerCmd.ModifyAmount(choiceContext, this, -SilenceCost, Owner, null);
        await sketchbook.TriggerSilenceRewrite(choiceContext);

        if (Owner.GetPower<AnanlinLiePower>() is { } liePower)
            await liePower.ResolveAfterSilenceRightClick(choiceContext);
    }

    public string RightClickPrompt => "消耗13层缄默。";
}
