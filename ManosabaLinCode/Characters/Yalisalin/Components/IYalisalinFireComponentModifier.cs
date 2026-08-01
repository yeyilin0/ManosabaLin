namespace ManosabaLin.Characters.Yalisalin.Components;

public interface IYalisalinFireComponentModifier
{
    void ModifyFireComponentConnectionPool(YalisalinFireComponentContext context) { }

    void ModifyFireComponentChoiceOptions(YalisalinFireComponentContext context) { }

    void ModifyFireComponentRightClickQueue(YalisalinFireComponentContext context) { }

    IEnumerable<string> GetFireComponentEnhancementDescriptions(Player owner)
    {
        return [];
    }

    Task AfterFireComponentChoiceCompleted(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        return Task.CompletedTask;
    }

    Task BeforeFireComponentBurned(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        return Task.CompletedTask;
    }

    Task AfterFireComponentBurned(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        return Task.CompletedTask;
    }

    Task AfterFireComponentResolved(PlayerChoiceContext choiceContext, YalisalinFireComponentContext context)
    {
        return Task.CompletedTask;
    }
}
