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
            && (Amount >= SilenceCost || HasNoahAssistCharge())
            && Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault()?.CanTriggerSilenceRewrite() == true;
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        var noahAssist = Owner.GetPower<AnanlinNoahAssistPower>();
        var useNoahAssist = noahAssist?.HasFreeRewriteCharge == true;
        if (!useNoahAssist && Amount < SilenceCost) return;
        if (Owner.Player?.Relics.OfType<AnansSketchbook>().FirstOrDefault() is not { } sketchbook) return;
        if (!sketchbook.CanTriggerSilenceRewrite()) return;

        if (!useNoahAssist)
            await PowerCmd.ModifyAmount(choiceContext, this, -SilenceCost, Owner, null);

        var rewrittenTargets = await sketchbook.TriggerSilenceRewriteAndGetTargets(choiceContext);
        if (useNoahAssist)
            await noahAssist!.ResolveFreeRewriteAttempt(choiceContext, rewrittenTargets, Owner, null);

        if (Owner.GetPower<AnanlinLiePower>() is { } liePower)
            await liePower.ResolveAfterSilenceRightClick(choiceContext);
    }

    private bool HasNoahAssistCharge()
    {
        return Owner.GetPower<AnanlinNoahAssistPower>()?.HasFreeRewriteCharge == true;
    }

    public string RightClickPrompt => "消耗13层缄默。";
}
