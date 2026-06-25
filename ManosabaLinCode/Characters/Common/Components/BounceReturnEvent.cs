namespace ManosabaLin.Characters.Common.Components;

public interface IBounceReturnListener
{
    Task OnBounceReturnedToHand(
        PlayerChoiceContext choiceContext,
        BounceReturnEvent bounceReturn)
    {
        return Task.CompletedTask;
    }
}

public sealed record BounceReturnEvent(
    Player Owner,
    CardModel Card,
    PileType PreviousPileType,
    BounceComponent Source);

internal static class BounceReturnEvents
{
    public static async Task DispatchReturnedToHand(
        PlayerChoiceContext choiceContext,
        ICombatState combatState,
        BounceReturnEvent bounceReturn)
    {
        foreach (var listener in OrderedListeners(combatState))
            await Dispatch(choiceContext, listener, l => l.OnBounceReturnedToHand(choiceContext, bounceReturn));
    }

    private static IEnumerable<IBounceReturnListener> OrderedListeners(ICombatState combatState)
    {
        return combatState.IterateHookListeners().OfType<IBounceReturnListener>().ToArray();
    }

    private static async Task Dispatch(
        PlayerChoiceContext choiceContext,
        IBounceReturnListener listener,
        Func<IBounceReturnListener, Task> dispatch)
    {
        var listenerModel = listener as AbstractModel;
        if (listenerModel != null)
            choiceContext.PushModel(listenerModel);

        try
        {
            await dispatch(listener);
            listenerModel?.InvokeExecutionFinished();
        }
        finally
        {
            if (listenerModel != null)
                choiceContext.PopModel(listenerModel);
        }
    }
}
